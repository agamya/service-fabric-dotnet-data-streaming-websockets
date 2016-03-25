// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Common.Shared
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Common.Logging;
    using Common.Model;

    public class BulkDataSource
    {
        public const string ProductsFile = "Common.Shared.Data.products.csv";
        private static readonly ILogger Logger = LoggerFactory.GetLogger(nameof(BulkDataSource));
        private static Random Rnd = new Random(DateTime.UtcNow.Millisecond);

        /// <summary>
        /// Reads all products from the baked in CSV file
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Product> ReadAllProducts()
        {
            Logger.Debug("reading list of products from CSV file...");


            List<Product> allProducts = this.ReadAllCsv(ProductsFile)
                .Skip(1) //skip CSV header
                .Select(
                    r => new Product(int.Parse(r[0]))
                    {
                        ProductNumber = r[1],
                        ProductName = r[2],
                        ModelName = r[3],
                        StandardCost = decimal.Parse(r[5]),
                        ListPrice = decimal.Parse(r[6]),
                        CategoryId = int.Parse(r[7]),
                        StockTotal = Rnd.Next(100, 1000),
                        StockReserved = 0
                    })
                .ToList();

            Logger.Debug("read {0} products", allProducts.Count);

            return allProducts;
        }

        /// <summary>
        /// Reads products from low Id to high Id from the baked in CSV file
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Product> ReadProducts(int lowId, int highId)
        {
            Logger.Debug("reading list of products from CSV file with productId from {0} to {1}...", lowId, highId);

            List<Product> allProducts = this.ReadAllCsv(ProductsFile)
                .Skip(1) //skip CSV header
                .Where(
                    r =>
                    {
                        int id = int.Parse(r[0]);
                        return id >= lowId && id <= highId;
                    })
                .Select(
                    r => new Product(int.Parse(r[0]))
                    {
                        ProductNumber = r[1],
                        ProductName = r[2],
                        ModelName = r[3],
                        StandardCost = decimal.Parse(r[5]),
                        ListPrice = decimal.Parse(r[6]),
                        CategoryId = int.Parse(r[7]),
                        StockTotal = Rnd.Next(100, 1000),
                        StockReserved = 0
                    })
                .ToList();

            Logger.Debug("Read {0} products", allProducts.Count);

            return allProducts;
        }

        private Stream GetCsvStream(string fileName)
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream(fileName);
        }

        private IEnumerable<string[]> ReadAllCsv(string fileName)
        {
            using (Stream s = this.GetCsvStream(fileName))
            {
                using (StreamReader reader = new StreamReader(s))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        List<string> values = line.Split(',').ToList();
                        for (int i = values.Count - 1; i > 0; i--)
                        {
                            if (values[i].EndsWith("\"") && values[i - 1].StartsWith("\""))
                            {
                                values[i - 1] = values[i - 1].Trim('"') + values[i].Trim('"');
                                values.RemoveAt(i);
                            }
                        }

                        yield return values.ToArray();
                    }
                }
            }
        }
    }
}