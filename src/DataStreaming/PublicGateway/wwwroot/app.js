angular.module('PublicGateway', [])
    .controller('DefaultController', function ($scope) {
        
        $scope.stocks = {};

        //Set the hubs URL for the connection
        $.connection.hub.url = "./signalr";

        // Declare a proxy to reference the hub: camel case=be very careful!
        var stockHubProxy = $.connection.stockHub;

        // Create a function that the hub can call to broadcast messages.
        stockHubProxy.client.notifyLowStockProducts = function (productStocks) {
            console.log("notifyLowStockProducts: productStocks: " + productStocks.length);

            productStocks.forEach(function (ps) {
                $scope.stocks[ps.ProductId.toString()] = {
                    productName: ps.ProductName,
                    stockLeft: ps.StockLeft,
                    reorder: ps.Reorder,
                    probability: ps.Probability
                };
            });
            
            $scope.$apply();
        };

        $.connection.hub.start()
            .done(function () { console.log('Now connected, connection ID=' + $.connection.hub.id); })
            .fail(function () { console.log('Could not Connect!'); });
    })

    // StockAggregatorController
    .controller('StockAggregatorController', function ($scope) {

        console.log('StockAggregatorController');

        $scope.numProbability = 0;
        $scope.products = [];

        var uri = 'stockaggregator/';

        $scope.getAllProducts = function() {
            jQuery.support.cors = true;
            $.ajax({
                url: uri + 'GetProducts',
                type: 'POST',
                dataType: 'json',
                success: function (data) {
                    console.log(data);
                    $scope.products = data;
                    $scope.$apply();
                },
                error: function(x, y, z) {
                    console.log('getAllProducts error: ' + x + '\n' + y + '\n' + z);
                }
            });
        }

        $scope.getHighProbabilityProducts = function () {
            jQuery.support.cors = true;
            var probability = $scope.numProbability;
            $.ajax({
                url: uri + 'GetHighProbabilityProducts/' + probability,
                type: 'POST',
                dataType: 'json',
                success: function (data) {
                    console.log(data);
                    $scope.products = data;
                    $scope.$apply();
                },
                error: function (x, y, z) {
                    console.log('getAllProducts error: ' + x + '\n' + y + '\n' + z);
                }
            });
        }
});