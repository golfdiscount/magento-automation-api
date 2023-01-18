using MySql.Data.MySqlClient;
using Pgd.Magento.Models;
using System.Collections.Generic;

namespace Pgd.Magento.Data
{
    /// <summary>
    /// Contains data access methods to interact with product information.
    /// </summary>
    public static class Products
    {
        /// <summary>
        /// Retrieves product information based on its SKU.
        /// </summary>
        /// <param name="sku">The SKU number of the product to retrieve</param>
        /// <param name="conn">Open connection to the database</param>
        /// <returns>Product information if the SKU is found in the database. Returns <see langword="null"/>
        /// if the SKU could not be found.</returns>
        /// <exception cref="MySqlException">Thrown when <paramref name="conn"/> is not open.</exception>
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

        /// <summary>
        /// Retrieves all products for an order.
        /// </summary>
        /// <param name="entityId">The entity ID of the order to retrieve products for. This is the 
        /// private, unique identifier for an order.</param>
        /// <param name="conn">Open connection to the database</param>
        /// <returns>A list of products for an order. Returns <see langword="null"/> if the
        /// order could not be found.</returns>
        /// <exception cref="MySqlException">Thrown when <paramref name="conn"/> is not open.</exception>
        public static List<LineItemModel> GetProductByOrder(int entityId, MySqlConnection conn)
        {
            using MySqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT sku, CAST(qty_ordered AS DECIMAL) AS quantity " +
                "FROM sales_order_item " +
                "WHERE order_id = @entity_id " +
                "AND product_type = 'simple';";
            cmd.Parameters.AddWithValue("@entity_id", entityId);

            using MySqlDataReader reader = cmd.ExecuteReader();

            if (!reader.HasRows)
            {
                return null;
            }

            // Mapping of SKU to quantity ordered
            Dictionary<string, int> skus = new();

            while (reader.Read())
            {
                skus.Add(reader.GetString("sku"), reader.GetInt32("quantity"));
            }

            reader.Close();

            List<LineItemModel> lineItems = new();

            foreach (string sku in skus.Keys)
            {
                ProductModel product = GetProduct(sku, conn);
                lineItems.Add(new()
                {
                    Name = product.Name,
                    Upc = product.Upc,
                    Description = product.Description,
                    Price = product.Price,
                    Sku = sku,
                    Quantity = skus[sku]
                });
            }

            return lineItems;
        }
    }
}
