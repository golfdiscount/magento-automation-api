using MySql.Data.MySqlClient;
using Pgd.Magento.Models;

namespace Pgd.Magento.Data
{
    /// <summary>
    /// Contains data access methods to interact with order information
    /// </summary>
    public static class Orders
    {
        /// <summary>
        /// Indicates the type of address.
        /// </summary>
        /// <value>Billing indicates that the address type is for the customer. Shipping
        /// means that the address type is for the recipient.</value>
        private enum AddressType
        {
            Billing,
            Shipping
        }

        /// <summary>
        /// Retrieves an order from the database based on the increment ID given.
        /// </summary>
        /// <param name="incrementId">Increment ID of the order. This is the public facing order number.</param>
        /// <param name="conn">Open connection to the database</param>
        /// <returns>Order information if the increment ID can be found in the database. Returns <see langword="null"/>
        /// if the order could not be found.</returns>
        /// <exception cref="MySqlException">Thrown when <paramref name="conn"/> is not open.</exception>
        public static OrderModel GetOrder(string incrementId, MySqlConnection conn)
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
            order.LineItems = Products.GetProductByOrder(order.Id, conn);

            return order;
        }

        /// <summary>
        /// Retrieves address information from the database based on an order's entity ID.
        /// </summary>
        /// <param name="entityId">Entity ID of the order which is the private,unique identifier for an order.</param>
        /// <param name="addressType">The <see cref="AddressType"/> type of address to look up.</param>
        /// <param name="conn">Open connection to the database</param>
        /// <returns>Address information if the entity ID has an associated address with it. Returns <see langword="null"/>
        /// if no address could be found.</returns>
        /// <exception cref="MySqlException">Thrown when <paramref name="conn"/> is not open.</exception>
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
            }
            else
            {
                cmd.Parameters.AddWithValue("@address_type", "billing");
            }

            using MySqlDataReader reader = cmd.ExecuteReader();

            if (!reader.HasRows)
            {
                return null;
            }

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
