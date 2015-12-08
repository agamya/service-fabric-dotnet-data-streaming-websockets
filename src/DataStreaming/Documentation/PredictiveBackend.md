# Service Fabric Predictive Backend Sample  #

This repository contains a sample project for Azure Service Fabric, the next-generation platform as a service offering from Microsoft. 

## Overview
This document walks you through how to deploy a the Predict Backend application, which demonstrates how to integrate the real time flow of orders from an e-commerce site and leverage Service Fabric to provide Stock Trend predictions.  The following key concepts are demonstrated in this sample: 

- Event streams: ingestion of data streams with ingress point exposed through Web Sockets.
- High-throughput service-to-service communication (using web sockets)
- Machine learning on real-time data streams



## Pre-Requisites
Visual Studio 2015

Before starting, make sure you have the Service Fabric development environment setup on your machine. Detailed instructions on how to setup the development environment can be found here https://azure.microsoft.com/en-gb/documentation/articles/service-fabric-get-started/ 
 
This solution is built against Service Fabric 4.4.87.9494 (install using web platform installer by searching for “Service Fabric”) 
Note - For full end to end run you will need to set up the Azure ML dependency, but the sample runs with “random” data generated to improve the F5 experience.  Please see “Setting up the Azure ML Dependency” if you want to deploy the full version including the back end Azure ML Service. 


## Download and building the source
Download the source and verify that you can build it in your local development environment by right clicking the DataStreaming solution and selecting Rebuild Solution. The solution uses the NuGet package manager to download and install its dependencies automatically.

Deploying the Solution to a Service Fabric Cluster 
- Right click on the PredictiveBackend Service Fabric application and select Publish 
- Select either your local cluster or your cloud deployment (requires that you have access to an Azure based Service fabric cluster).  For the purposes of these instructions we’ll assume you are deploying to a local dev box Service Fabric cluster
- Click Publish and validate the application deploys successfully. 
- Open Service Fabric Explorer and validate that the cluster and the PrectiveBackendType application and all sub-services are healthy

 

## Run The Test Client
Open DataStreaming\Tests\PublicGateway.Test\bin\x64\Debug and run PublicGateway.Test.exe.  If you run it with no command line argument parameters in a command prompt window it provides instructions on how to run it in one of 3 modes 

Purchase: place orders over the Web API Controller 

Reorder: reorder specified stock 

Stream: Uses web sockets to place orders

## Run The Web Page
Once the solution is deployed we provide two views implemented using Angular and hosted in the PublicGateway stateless service  
- Products in stock - Real time updates http://localhost:3251/
- Aggregated view of stock trend predictions http://localhost:3251/StockAggregator.html note there is a link to this from the main page 


## Setting Up Azure ML
The default implementation of the AzureML client is a mocked out implementation that generates a random value.  This setting is determined by the file:
src\DataStreaming\StockTrendPredictionActor\PackageRoot\Config\Settings.xml

if you open this file you should see a section AzureML, which contains Parameter Client with value "Mocked".  If you change this value to "Azure" then you will also need to update the following two values 
AzureMlApiUrl
AzureMlKey
 
This section will talk you through how to set up the fully functioning Azure ML model that this sample can integrate with 

### Azure Machine Learning – Stock Re-order Prediction
Azure Machine Learning is a useful tool to make predictions from your data.  It provides a managed cloud based solution for predictive analytics solutions that is both easy to use and incredibly powerful.  We have provided a sample experiment to use as part of this E-Commerce example that will make predictions on whether stock should be re-ordered based on historical consumption rates and stock levels amongst other features.  To set this up perform the following steps 

1. In a browser navigate to ⦁	http://manage.windowsazure.com
2. Select an appropriate subscription 
3. Select New
4. Select Data Services -> Machine Learning -> Quick Create
5. Enter a name for the workspace, select an appropriate Location and Subscription and create a new storage account that the workspace will use to store its resources
6. Click Create An ML Workspace to complete this step
7. In a Browser navigate to the URI: http://gallery.azureml.net/Details/8c0cef5b004b4e23b2b7fd2b34c474c8
8. Select Open In Studio, this will open a new dialogue, select the Region in which you created the workspace and the newly created workspace itself, Click Ok
9. The experiment will be imported into your workspace
10. Click Run to run the experiment
11. The experiment should run successfully without error
12. Once the experiment has been run click the Set Up Web Service button
13. This will generate two new objects in the experiment, a web service input and output
14. Reconnect the input element to the second input of the Score Model object
15. Reconnect the output element to the output of the Score Model object
16. Re-run the experiment and once completed click the Deploy Web Service button
17. The web service will then be deployed and a new view shown
18. To test the web service click the “Test” button 
19. A new dialogue will open, enter some values and click Ok (For example Choose Item 742, Set Consumption Rate to 4.1, Set Average Time Between Purchases to 54000, Average Purchase Size to 3.1, Stock Level to 25 and Total Orders to 110). Click Ok
20. Once the test has completed you can view the results, in this case the experiment predicted stock will need to be replenished with 62% certainty
21. Copy the ML API Key for the Web Service to the Service Fabric Solution in the location described at the beginning of this section (settings file in Stock Trend Prediction Actor) 
22. Copy the web service Request URI click Request/Response under Default Endpoint on the Dashboard for the Service in ML Studio.  Importantly remember to delete “&details=true” from the string you copy.  Example URI shown below. https://europewest.services.azureml.net/workspaces/f5bffed8193647de88b3f1123da21a2a/services/b76e4b0cf7fc49a69b81619e9eadedc0/execute?api-version=2.0&details=true **Note reminder delete from &details=true before pasting in settings.xml**









