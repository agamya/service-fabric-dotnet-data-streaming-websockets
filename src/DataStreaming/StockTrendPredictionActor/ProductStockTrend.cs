// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace StockTrendPredictionActor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;
    using Common.Model;
    using global::StockTrendPredictionActor.Interfaces;

    /// <summary>
    /// Part of the StockTrendPredictionActor
    /// </summary>
    [DataContract]
    public class ProductStockTrend
    {
        [DataMember] public List<PurchaseTag> PurchaseHistory;

        [DataMember] public DateTime MinDate;

        [DataMember] public DateTime MaxDate;

        [DataMember] public bool Reorder;

        [DataMember] public int NotificationCounter;

        [DataMember]
        public DateTime LastOrderTimestamp { get; set; }

        [DataMember]
        public int ProductId { get; set; }

        [DataMember]
        public string ProductName { get; set; }

        [DataMember]
        public int LastStockCount { get; set; }

        [DataMember]
        public float Probability { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double AvgPurchasesPerDay => (double) this.TotalPurchases/(double) (this.MaxDate - this.MinDate).TotalDays;

        public double AvgTimeBetweenPurchases
        {
            get
            {
                double rollingSum = 0.0;
                for (int x = 0; x < this.PurchaseHistory.Count - 1; x++)
                {
                    rollingSum += (this.PurchaseHistory[x + 1].Date - this.PurchaseHistory[x].Date).TotalSeconds;
                }

                return (double) rollingSum/(double) this.PurchaseHistory.Count;
            }
        }

        public double AvgQuantityPerOrder => this.TotalPurchases/(double) this.PurchaseHistory.Count;

        public int TotalPurchases
        {
            get { return this.PurchaseHistory.Sum(o => o.Quantity); }
        }

        public void Reset(DateTime minDate, DateTime maxDate)
        {
            this.MinDate = minDate;
            this.MaxDate = maxDate;

            if (this.PurchaseHistory == null)
            {
                this.PurchaseHistory = new List<PurchaseTag>();
            }

            this.PurchaseHistory = this.PurchaseHistory.Where(o => o.Date > minDate).ToList();
        }

        public void AddOrder(ProductPurchase purchase, int notificationAttempts)
        {
            if (this.PurchaseHistory == null)
            {
                this.PurchaseHistory = new List<PurchaseTag>();
            }

            this.PurchaseHistory.Add(new PurchaseTag {Date = purchase.Timestamp, Quantity = purchase.Quantity});

            if (purchase.Timestamp > this.LastOrderTimestamp)
            {
                this.LastStockCount = purchase.StockLeft;
                this.NotificationCounter = notificationAttempts;
                this.LastOrderTimestamp = purchase.Timestamp;
            }
        }

        public ProductStockPrediction ToProductPrediction()
        {
            return new ProductStockPrediction()
            {
                ProductId = this.ProductId,
                ProductName = this.ProductName,
                StockLeft = this.LastStockCount,
                Reorder = this.Reorder,
                Probability = this.Probability
            };
        }

        [DataContract]
        public class PurchaseTag
        {
            [DataMember]
            public int Quantity { get; set; }

            [DataMember]
            public DateTime Date { get; set; }
        }
    }
}