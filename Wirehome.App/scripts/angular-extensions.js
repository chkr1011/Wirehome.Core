var extendAngularScope = function ($scope) {
    $scope.getNumbers = function (num) {
        var a = new Array(num + 1);
        for (var i = 0; i < num + 1; i++) {
            a[i] = i;
        }

        return a;
    }
};