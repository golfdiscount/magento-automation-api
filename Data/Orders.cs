using magestack.Models;
using MySql.Data.MySqlClient;

namespace magestack.Data
{
    public static class Orders
    {
        private enum AddressType
        {
            Shipping,
            Billing
        }

        public static OrderModel GetOrder (string incrementId, MySqlConnection conn)
        {
            MySqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT sales_order.entity_id, " +
                "increment_id, " +
                "state, " +
                "status, " +
                "shipping_description, " +
                "FORMAT(sales_order.base_grand_total, 2) AS 'total', " +
                "FORMAT(sales_order.base_shipping_amount, 2) AS 'shipping_total', " +
                "created_at, " +
                "updated_at, " +
                "payment.method " +
                "FROM sales_order " +
                "JOIN sales_order_payment AS payment ON payment.parent_id = sales_order.entity_id " +
                "WHERE increment_id = @increment_id;";
            cmd.Parameters.AddWithValue("@increment_id", incrementId);

            using MySqlDataReader reader = cmd.ExecuteReader();
            
            if (!reader.HasRows)
            {
                return null;
            }

            reader.Read();

            OrderModel order = new()
            {
                Id = reader.GetInt32("entity_id"),
                OrderNumber = reader.GetString("increment_id"),
                State = reader.GetString("state"),
                Status = reader.GetString("status"),
                Shipping = reader.GetString("shipping_description"),
                PaymentMethod = reader.GetString("method"),
                OrderTotal = reader.GetDecimal("total"),
                ShippingTotal = reader.GetDecimal("shipping_total"),
                CreatedAt = reader.GetDateTime("created_at"),
                UpdatedAt = reader.GetDateTime("updated_at")
            };

            reader.Close();

            order.Customer = GetAddress(order.Id, AddressType.Billing, conn);
            order.Recipient = GetAddress(order.Id, AddressType.Shipping, conn);
            order.Products = Products.GetProductByOrder(order.Id, conn);

            return order;
        }
        private static AddressModel GetAddress(int entityId, AddressType addressType, MySqlConnection conn)
        {
            using MySqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT CONCAT(firstname, ' ', lastname) AS 'name'," +
                "street," +
                "city," +
                "region AS 'state'," +
                "country_id AS 'country'," +
                "postcode AS 'zip' " +
                "FROM sales_order_address " +
                "WHERE parent_id = @entity_id " +
                "AND address_type = @address_type;";
            cmd.Parameters.AddWithValue("@entity_id", entityId);

            if (addressType == AddressType.Shipping)
            {
                cmd.Parameters.AddWithValue("@address_type", "shipping");
            } else
            {
                cmd.Parameters.AddWithValue("@address_type", "billing");
            }           

            using MySqlDataReader reader = cmd.ExecuteReader();
            reader.Read();

            AddressModel address = new()
            {
                Name = reader["name"].ToString(),
                Street = reader["street"].ToString(),
                City = reader["city"].ToString(),
                State = reader["state"].ToString(),
                Country = reader["country"].ToString(),
                Zip = reader["zip"].ToString()
            };

            return address;
        }
    }
}
