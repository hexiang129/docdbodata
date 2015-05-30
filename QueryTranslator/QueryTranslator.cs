// -----------------------------------------------------------------------
//      The code is copied from the NHibernateQueryableSample sample from
//      ASP.Net web stack sample repository from http://aspnet.codeplex.com/
//      and customized for document db
//      the odata filter grammar can be found here 
//      http://docs.oasis-open.org/odata/odata/v4.0/odata-v4.0-part2-url-conventions.html
// -----------------------------------------------------------------------
//Install-Package Microsoft.OData.Core
//Install-Package Microsoft.AspNet.OData
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.OData.Query;
using System.Web.OData.Extensions;
using Microsoft.OData.Core.UriParser.Semantic;
using Microsoft.OData.Core.UriParser.TreeNodeKinds;
using Microsoft.OData.Edm;

namespace QueryTranslator
{
    /// <summary>
    /// this class holds the information necessary to construct a sql query
    /// </summary>
    public class WhereAndJoinClause
    {
        /// <summary>
        /// the sql query should contain "where " followed by Clause string
        /// </summary>
        public string Clause { get; set; }

        /// <summary>
        /// the "any" odata operator will be translated to self join operation 
        /// in documentdb sql string
        /// Note: current document db only allows 2 join clause
        /// </summary> 
        public List<string> JoinClause { get; set; }
    }

    /// <summary>
    /// generate a document db query from an odata query
    /// </summary>
    public class DocDBQueryGenerator
    {
        private WhereAndJoinClause whereAndJoinClause;

        public DocDBQueryGenerator()
        {
            whereAndJoinClause = null;
        }

        /// <summary>
        /// Generate document db sql query that
        /// queries a set of items 
        /// with "where" clause generated from odata query in queryOptions object.
        /// </summary>
        /// <returns>the generated docdb sql query in a string. 
        /// The generated query is logged in applicatio insight as a trace event.</returns>
        public string TranslateToDocDBQuery<T>(ODataQueryOptions<T> queryOptions) where T : class
        {
            string query;

            if (queryOptions.Filter != null)
            {
                WhereAndJoinClause where = TranslateODataFilterToDocDBWhereAndJoin(queryOptions.Filter);
                string ItemTypeFilterClause = string.Empty;

                if (where.JoinClause.Any())
                {
                    string j = string.Empty;
                    foreach (var str in where.JoinClause)
                    {
                        j += str + " ";
                    }

                    query = string.Format("SELECT c FROM c {0} WHERE {2} ({1})", j, where.Clause, ItemTypeFilterClause);
                }
                else
                {
                    query = string.Format("SELECT c FROM c WHERE {1} ({0})", where.Clause, ItemTypeFilterClause);
                }
            }
            else
            {
                query = string.Format("SELECT c FROM c");
            }
            return query;
        }

        /// <summary>
        /// translate an odata filter clause to a documentdb where and self join clause.
        /// </summary>
        /// <param name="filterQuery">odata filter option object which contains a AST of odata filter clause</param>
        /// <returns>WhereAndJoinClause object that contains the where clause and a list of self join clauses.
        /// if filterQuery is null, it will return an empty where clause and an empty list of self join clauses.</returns>
        public WhereAndJoinClause TranslateODataFilterToDocDBWhereAndJoin(FilterQueryOption filterQuery)
        {
            whereAndJoinClause = new WhereAndJoinClause
            {
                Clause = String.Empty,
                JoinClause = new List<string>()
            };

            RangeVariablesNameForAnyNodeBody = new Stack<string>();

            if (filterQuery != null)
            {
                if (filterQuery.FilterClause != null && filterQuery.FilterClause.Expression != null)
                {
                    whereAndJoinClause.Clause = Translate(filterQuery.FilterClause.Expression);
                    // JoinClause will be filled in by TranslateFilter through the static instance whereAndJoinClause
                }
            }

            return whereAndJoinClause;
        }

        /// <summary>
        /// The input is a node in the AST of odata filter caluse.
        /// This function computes the where and self-join docdb query clauses
        /// for the subtree whose root is this node, 
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private string Translate(QueryNode node)
        {
            CollectionNode collectionNode = node as CollectionNode;
            SingleValueNode singleValueNode = node as SingleValueNode;

            if (collectionNode != null)
            {
                switch (node.Kind)
                {
                    case QueryNodeKind.CollectionNavigationNode:
                        CollectionNavigationNode navigationNode = node as CollectionNavigationNode;
                        return TranslateNavigationPropertyNode(navigationNode.Source, navigationNode.NavigationProperty);

                    case QueryNodeKind.CollectionPropertyAccess:
                        return TranslateCollectionPropertyAccessNode(node as CollectionPropertyAccessNode);
                }
            }
            else if (singleValueNode != null)
            {
                switch (node.Kind)
                {
                    case QueryNodeKind.BinaryOperator:
                        return TranslateBinaryOperatorNode(node as BinaryOperatorNode);

                    case QueryNodeKind.Constant:
                        return TranslateConstantNode(node as ConstantNode);

                    case QueryNodeKind.Convert:
                        return TranslateConvertNode(node as ConvertNode);

                    case QueryNodeKind.EntityRangeVariableReference:
                        return TranslateRangeVariable((node as EntityRangeVariableReferenceNode).RangeVariable);

                    case QueryNodeKind.NonentityRangeVariableReference:
                        return TranslateRangeVariable((node as NonentityRangeVariableReferenceNode).RangeVariable);

                    case QueryNodeKind.SingleValuePropertyAccess:
                        return TranslatePropertyAccessQueryNode(node as SingleValuePropertyAccessNode);

                    case QueryNodeKind.UnaryOperator:
                        return TranslateUnaryOperatorNode(node as UnaryOperatorNode);

                    // single value function are like "month", "concat", "floor", "ceiling". 
                    // an example will be $filter=endswith(Description, 'abc')
                    //case QueryNodeKind.SingleValueFunctionCall:
                    //    return TranslateSingleValueFunctionCallNode(node as SingleValueFunctionCallNode);

                    // suppose RackSku has a member called dummy of type ServerSku
                    // then the "dummy" part in $filter=dummy/MSFId eq 12 is a SingleNavigationNode
                    case QueryNodeKind.SingleNavigationNode:
                        SingleNavigationNode navigationNode = node as SingleNavigationNode;
                        return TranslateNavigationPropertyNode(navigationNode.Source, navigationNode.NavigationProperty);

                    case QueryNodeKind.Any:
                        return TranslateAnyNode(node as AnyNode);

                    // odata all operator is not supported.
                    //case QueryNodeKind.All:
                    //    return TranslateAllNode(node as AllNode);
                }
            }

            throw new NotSupportedException(String.Format("Nodes of type {0} are not supported", node.Kind));
        }

        /// <summary>
        /// BGs, DCs are examples of CollectionPropertyAccessNode
        /// </summary>
        private string TranslateCollectionPropertyAccessNode(CollectionPropertyAccessNode collectionPropertyAccessNode)
        {
            return Translate(collectionPropertyAccessNode.Source) + "." + collectionPropertyAccessNode.Property.Name;
        }


        private string TranslateNavigationPropertyNode(SingleValueNode singleValueNode, IEdmNavigationProperty edmNavigationProperty)
        {
            return Translate(singleValueNode) + "." + edmNavigationProperty.Name;
        }

        /// <summary>
        /// Since SkuBomParents/any(m: m eq 1363) will be translated to
        /// join d0 in SkuBomParents were d0 = 1363
        /// The range variable "m" needs to be replaced with "d0"
        /// the top element of RangeVariablesNameForAnyNodeBody stores the 
        /// range variable name for the current Any operator body
        /// (note that any operator body can be nested)
        /// </summary>
        private Stack<string> RangeVariablesNameForAnyNodeBody;

        /// <summary>
        /// in AnyNode: such as SkuBomParents/any(m: m eq 1363)
        /// anyNode.Source is SkuBomParents, which is a CollectionPropertyAccessNode.
        /// anyNode.Body is m eq 1363, which is a BinaryOperatorNode.
        /// For $filter=SkuBomParents/any(m: m eq 1363)  
        /// it translates to 
        /// 1 self join clause: join d0 in c.SkuBomParents
        /// the where clause:   d0 = 1363
        /// For $filter=BGs/any(m: m eq '640') and DCs/any(m: m eq 'ACT01')
        /// it translates to
        /// 2 self join clauses: 
        /// join d0 in c.BGs
        /// join d1 in c.DCs
        /// and the where clause is (d0 = "640") and (d1="ACT01")
        /// For SkuChildren/any(m:m/dummy/any(n:n eq 'abc'))
        /// it translates to 
        /// 2 self join clauses:
        /// join d0 in c.SkuChildren 
        /// join d1 in d0.dummy  
        /// and the where clause is ((d1 = "abc"))
        /// </summary>
        private string TranslateAnyNode(AnyNode anyNode)
        {
            var cnt = whereAndJoinClause.JoinClause.Count();
            var rangeVariableName = string.Format("d{0}", cnt);
            var src = Translate(anyNode.Source);
            whereAndJoinClause.JoinClause.Add(string.Format("join {0} in {1}", rangeVariableName, src));
            RangeVariablesNameForAnyNodeBody.Push(rangeVariableName);
            var res = Translate(anyNode.Body);
            RangeVariablesNameForAnyNodeBody.Pop();
            return res;
        }

        /// <summary>
        /// For example,  not IsPlanning  is parsed as a unaryOperatorNode
        /// unaryOperatorNode.OperatorKind is UnaryOperatorKind.Not, which will be translated to NOT
        /// unaryOperatorNode.Operand is IsPlanning, which is a SingleValuePropertyAccessNode, and will be
        /// translated to c.IsPlanning
        /// So this function will return (NOT (c.IsPlanning))
        /// </summary>
        private string TranslateUnaryOperatorNode(UnaryOperatorNode unaryOperatorNode)
        {
            return ToString(unaryOperatorNode.OperatorKind) + "(" + Translate(unaryOperatorNode.Operand) + ")";
        }

        /// <summary>
        /// ItemType, State, Entity1/a_property_of_Entity2 etc are all SingleValuePropertyAccessNode
        /// Suppose RackSku has a property of type ServerSku with name "server"
        /// Then one can query /MsfItems/SkuReference.RackSku?$filter=server/MSFId eq 12
        /// server/MSFId eq 12 is a BinaryOperatorNode
        /// whose left node is "server/MSFId" which is a SingleValuePropertyAccessNoe
        /// The property name is MSFId
        /// "server" is a SingleNavigationNode
        /// The navigation property name is "server",
        /// and the source of navigation property is the implicit variable "$it" referring to RackSku
        /// $it is of type EntityRangeVariable.
        /// </summary>
        private string TranslatePropertyAccessQueryNode(SingleValuePropertyAccessNode singleValuePropertyAccessNode)
        {
            return Translate(singleValuePropertyAccessNode.Source) + "."
                + singleValuePropertyAccessNode.Property.Name;
        }

        /// <summary>
        /// "m" is the range variable in expression SkuBomParents/any(m: m eq 1363)
        /// if m is an entity, then it is an EntityRangeVariable
        /// if m is a complex type/primitive type, then it is a NonentityRangeVariable
        /// </summary>
        private string TranslateRangeVariable(NonentityRangeVariable nonentityRangeVariable)
        {
            //override the range variable name if we are inside the body clause of an any node
            if (RangeVariablesNameForAnyNodeBody.Any())
            {
                return RangeVariablesNameForAnyNodeBody.Peek();
            }
            return nonentityRangeVariable.Name.ToString();
        }

        /// <summary>
        /// see comments for TranslateRangeVariable
        /// </summary>
        private string TranslateRangeVariable(EntityRangeVariable entityRangeVariable)
        {
            var res = entityRangeVariable.Name.ToString();
            //odata section 5.1.1.6.4, $it is a range variable indicating current entity queried against
            if (res == "$it")
            {
                return "c";
            }

            //override the range variable name if we are inside the body clause of an any node
            if (RangeVariablesNameForAnyNodeBody.Any())
            {
                return RangeVariablesNameForAnyNodeBody.Peek();
            }
            return res;
        }

        /// <summary>
        /// this appears in filter 
        /// $filter=MiniPorts gt 20 and SkuPowerAt100pctLoadW gt 88000
        /// the convertNode represents the part SkuPowerAt100pctLoadW gt 88000, which is nullable boolean.
        /// </summary>
        private string TranslateConvertNode(ConvertNode convertNode)
        {
            return Translate(convertNode.Source);
        }

        /// <summary>
        /// string abc will be translated to \"abc\"
        /// bool will be casted to lower case string. (Docdb does not recognize False, but recognize false)
        /// </summary>
        private string TranslateConstantNode(ConstantNode constantNode)
        {
            var val = constantNode.Value;
            if (val is string)
            {
                return string.Format("\"{0}\"", val);
            }
            if (val is Microsoft.OData.Core.ODataEnumValue)
            {
                return ((Microsoft.OData.Core.ODataEnumValue)(val)).Value;
            }
            var res = constantNode.Value.ToString();
            if (res == "False") return "false";
            if (res == "True") return "true";
            return res;
        }

        private string TranslateBinaryOperatorNode(BinaryOperatorNode binaryOperatorNode)
        {
            var left = Translate(binaryOperatorNode.Left);
            var right = Translate(binaryOperatorNode.Right);
            return "(" + left + " " + ToString(binaryOperatorNode.OperatorKind) + " " + right + ")";
        }

        private string ToString(BinaryOperatorKind binaryOpertor)
        {
            switch (binaryOpertor)
            {
                case BinaryOperatorKind.Add:
                    return "+";
                case BinaryOperatorKind.And:
                    return "AND";
                case BinaryOperatorKind.Divide:
                    return "/";
                case BinaryOperatorKind.Equal:
                    return "=";
                case BinaryOperatorKind.GreaterThan:
                    return ">";
                case BinaryOperatorKind.GreaterThanOrEqual:
                    return ">=";
                case BinaryOperatorKind.LessThan:
                    return "<";
                case BinaryOperatorKind.LessThanOrEqual:
                    return "<=";
                case BinaryOperatorKind.Modulo:
                    return "%";
                case BinaryOperatorKind.Multiply:
                    return "*";
                case BinaryOperatorKind.NotEqual:
                    return "!=";
                case BinaryOperatorKind.Or:
                    return "OR";
                case BinaryOperatorKind.Subtract:
                    return "-";
                default:
                    return null;
            }
        }

        private string ToString(UnaryOperatorKind unaryOperator)
        {
            switch (unaryOperator)
            {
                case UnaryOperatorKind.Negate:
                    return "!";
                case UnaryOperatorKind.Not:
                    return "NOT";
                default:
                    return null;
            }
        }
    }
}
