using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

// only properties will be reflected in the odata model
namespace Model
{
    public class FamilyMember
    {
        public string familyName{get; set;}
        public string givenName{get; set;}
    }

    public class Parent : FamilyMember
    {
    }

    public class Pet
    {
        public string givenName{get; set;}
    }

    public enum Gender
    {
        Male,
        Female
    }

    public class Child : FamilyMember
    {
        public Gender gender{get; set;}
        public int grade{get; set;}
        public List<Pet> pets{get; set;}
    }

    public class Address
    {
        public string state{get; set;}
        public string county{get; set;}
        public string city { get; set; }
    }

    /// <summary>
    /// this is the data model we use in docdb.
    /// </summary>
    public class Family
    {
        [Key]
        public string id { get; set; }
        public List<Parent> parents { get; set; }

        public int parentsCount { get; set; }

        public List<Child> children { get; set; }

        public int childrenCount { get; set; }

        public Address address { get; set; }
        public bool isRegistered { get; set; }
    }

    /// <summary>
    /// this model is not used to store data.
    /// We use it to create controller which returns translated docdb query string
    /// </summary>
    public class FamilyForTest
    {
        [Key]
        public string id { get; set; }
        public List<Parent> parents { get; set; }

        public int parentsCount { get; set; }

        public List<Child> children { get; set; }

        public int childrenCount { get; set; }

        public Address address { get; set; }
        public bool isRegistered { get; set; }
    }
}
