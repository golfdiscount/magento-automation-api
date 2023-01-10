using MySql.Data.MySqlClient;
using Pgd.Magento.Models;
using System.Collections.Generic;

namespace Pgd.Magento.Data
{
    public static class Products
    {
        public static ProductModel GetProduct(string sku, MySqlConnection conn)
        {
            MySqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = $@"SELECT v1.value AS 'name',
                    e.sku,
                    FORMAT(d1.value, 2) AS 'price',
                    t1.value AS 'short_description',
                    v2.value AS 'upc'
                FROM catalog_product_entity e
                LEFT JOIN catalog_product_entity_varchar v1 ON e.entity_id = v1.entity_id
                AND v1.store_id = 0
                AND v1.attribute_id =(
                    SELECT attribute_id
                    FROM eav_attribute
                    WHERE attribute_code = 'name'
                    AND entity_type_id = (
                        SELECT entity_type_id
                        FROM eav_entity_type
                        WHERE entity_type_code = 'catalog_product'))
                LEFT JOIN catalog_product_entity_text t1 ON e.entity_id = t1.entity_id
                AND t1.store_id = 0
                AND t1.attribute_id = (
                    SELECT attribute_id
                    FROM eav_attribute
                    WHERE attribute_code = 'short_description'
                    AND entity_type_id = (
                        SELECT entity_type_id
                        FROM eav_entity_type
                        WHERE entity_type_code = 'catalog_product'))
                LEFT JOIN catalog_product_entity_decimal d1 ON e.entity_id = d1.entity_id
                AND d1.store_id = 0
                AND d1.attribute_id = (
                    SELECT attribute_id
                    FROM eav_attribute
                    WHERE attribute_code = 'price'
                    AND entity_type_id = (
                        SELECT entity_type_id
                        FROM eav_entity_type
                        WHERE entity_type_code = 'catalog_product'))
                LEFT JOIN catalog_product_entity_varchar v2 ON e.entity_id = v2.entity_id
                AND v2.store_id = 0
                AND v2.attribute_id =(
	                SELECT attribute_id
	                FROM eav_attribute
	                WHERE attribute_code = 'upc'
		                AND entity_type_id = (
			                SELECT entity_type_id
			                FROM eav_entity_type
			                WHERE entity_type_code = 'catalog_product'))
                WHERE e.sku = @sku;";
            cmd.Parameters.AddWithValue("@sku", sku);

            using MySqlDataReader reader = cmd.ExecuteReader();

            if (!reader.HasRows)
            {
                return null;
            }

            reader.Read();

            ProductModel product = new()
            {
                Name = reader.IsDBNull(0) ? null : reader.GetString("name"),
                Sku = reader.IsDBNull(1) ? null : reader.GetString("sku"),
                Price = reader.IsDBNull(2) ? 0 : reader.GetDecimal("price"),
                Description = reader.IsDBNull(3) ? null : reader.GetString("short_description"),
                Upc = reader.IsDBNull(4) ? null : reader.GetString("upc")
            };

            return product;
        }

        public static List<ProductModel> GetProductByOrder(int entity_id, MySqlConnection conn)
        {
            using MySqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT DISTINCT sku FROM sales_order_item WHERE order_id = @entity_id;";
            cmd.Parameters.AddWithValue("@entity_id", entity_id);

            using MySqlDataReader reader = cmd.ExecuteReader();

            if (!reader.HasRows)
            {
                return null;
            }

            List<string> skus = new();

            while (reader.Read())
            {
                skus.Add(reader.GetString("sku"));
            }

            reader.Close();

            List<ProductModel> products = new();

            foreach (string sku in skus)
            {
                products.Add(GetProduct(sku, conn));
            }

            return products;
        }
    }
}
