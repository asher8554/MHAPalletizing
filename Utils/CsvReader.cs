using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MHAPalletizing.Models;

namespace MHAPalletizing.Utils
{
    /// <summary>
    /// CSV 파일에서 주문 데이터를 읽어오는 유틸리티
    /// Dataset 형식: Order,Product,Quantity,Length,Width,Height,Weight
    /// </summary>
    public class CsvReader
    {
        /// <summary>
        /// CSV 파일에서 주문 목록을 읽어옵니다.
        /// </summary>
        /// <param name="filePath">CSV 파일 경로</param>
        /// <returns>주문 목록</returns>
        public static List<Order> ReadOrdersFromCsv(string filePath)
        {
            var orders = new Dictionary<string, Order>();

            using (var reader = new StreamReader(filePath))
            {
                // 헤더 스킵
                string headerLine = reader.ReadLine();

                int itemIdCounter = 1;

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(',');

                    if (values.Length < 7)
                        continue;

                    string orderId = values[0].Trim();
                    string productId = values[1].Trim();
                    int quantity = int.Parse(values[2].Trim());
                    double length = double.Parse(values[3].Trim());
                    double width = double.Parse(values[4].Trim());
                    double height = double.Parse(values[5].Trim());
                    double weight = double.Parse(values[6].Trim());

                    // 주문이 없으면 새로 생성
                    if (!orders.ContainsKey(orderId))
                    {
                        orders[orderId] = new Order(orderId);
                    }

                    // Quantity만큼 아이템 생성
                    for (int i = 0; i < quantity; i++)
                    {
                        var item = new Item(productId, itemIdCounter++, length, width, height, weight);
                        orders[orderId].Items.Add(item);
                    }
                }
            }

            return orders.Values.ToList();
        }

        /// <summary>
        /// CSV 파일에서 특정 주문만 읽어옵니다.
        /// </summary>
        public static Order ReadSingleOrder(string filePath, string orderId)
        {
            var allOrders = ReadOrdersFromCsv(filePath);
            return allOrders.FirstOrDefault(o => o.OrderId == orderId);
        }

        /// <summary>
        /// CSV 파일의 주문 ID 목록을 반환합니다.
        /// </summary>
        public static List<string> GetOrderIds(string filePath)
        {
            var orderIds = new HashSet<string>();

            using (var reader = new StreamReader(filePath))
            {
                // 헤더 스킵
                reader.ReadLine();

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(',');
                    if (values.Length > 0)
                    {
                        orderIds.Add(values[0].Trim());
                    }
                }
            }

            return orderIds.ToList();
        }
    }
}
