(function () {
    var familyApp = angular.module('familyApp', ['ngResource']);
    familyApp.factory('familySvc', ['$resource', function ($resource) {
        var url = "/odata/Families";
        var resource = $resource(url, {},
            {
                'query': { method: "GET", url: "/odata/Families" },
                'translate': { method: "GET", url: "/odata/FamilyForTests" }
            });
        return resource;
    }]);

    familyApp.controller('familyCtrl', function ($scope, $http, familySvc) {
        var self = this;
        self.data = "";
        $scope.dataType = "";
        self.IsRunning = false;
        self.TranslatorRunning = false;

        self.QueryFamilyRecordsBasedonPetName = function (petName) {
            $scope.querystring = "children/any(m:m/pets/any(n:n/givenName eq '"+ angular.uppercase(petName) + "'))";
            self.QueryFamilyRecords();
        }

        self.QueryFamilyRecordsBasedonGenderAndState = function (gender, state) {
            $scope.querystring = "children/any(m:m/gender eq Model.Gender'" + gender + "') and address/state eq '"
                + state +"'";
            self.QueryFamilyRecords();
        }

        self.QueryFamilyRecords = function () {
            self.IsRunning = true;
            self.nextlink = "";
            familySvc.query({ $filter: $scope.querystring },
                 function (data, headers) {
                     self.data = data.value;
                     if (data["@odata.nextLink"]) {
                         self.nextlink = data["@odata.nextLink"];
                     }
                     $scope.dataType = typeof data;
                     self.IsRunning = false;
                 },
                function (data, headers) {
                    self.data = data.data.error.message;
                    $scope.dataType = "message";
                    self.IsRunning = false;
                });

            self.TranslatorRunning = true;
            familySvc.translate({ $filter: $scope.querystring },
                 function (data, headers) {
                     self.TranslationResult = data.value;
                     self.TranslatorRunning = false;
                 },
                function (data, headers) {
                    self.TranslationResult = "Translation Failed:" + data.data.error.message;
                    self.TranslatorRunning = false;
                });
        }

        self.RawQuery = function () {
            self.IsRunning = true;
            $http.get(self.nextlink)
            .success(function (data, status, headers, config) {
                self.data = data.value;
                if (data["@odata.nextLink"]) {
                    self.nextlink = data["@odata.nextLink"];
                } else
                {
                    self.nextlink = "";
                }
                $scope.dataType = typeof data;
                self.IsRunning = false;
            })
            .error(function (data, status, headers, config) {
                self.nextlink = "";
                self.data = data.data.error.message;
                $scope.dataType = "message";
                self.IsRunning = false;
            });
        }
    });

    familyApp.controller('navigationCtrl', function ($scope) {
        var self = this;
        self.SelectedPage = null;

        self.SelectedSection = function (name) {
            self.SelectedPage = name;
        };

        self.IsSelected = function (name) {
            return self.SelectedPage === name;
        };
    });

    familyApp.controller('dropDownCtrl', function ($scope) {
        $scope.genders = ['Male', 'Female'];
        $scope.states = ['AL', 'AK', 'AZ', 'AR', 'CA', 'CO', 'CT', 'DE', 'FL', 'GA', 'HI', 'ID', 'IL', 'IN', 'IA', 'KS', 'KY', 'LA',
                        'ME', 'MD', 'MA', 'MI', 'MN', 'MS', 'MO', 'MT', 'NE', 'NV', 'NH', 'NJ', 'NM', 'NY', 'NC', 'ND', 'OH', 'OK', 'OR',
                        'PA', 'RI', 'SC', 'SD', 'TN', 'TX', 'UT', 'VT', 'VA', 'WA', 'WV', 'WI', 'WY'];
        $scope.selectedGender = $scope.genders[0];
        $scope.selectedState = $scope.states[0];
    });
})();