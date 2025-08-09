using MySqlConnector;
using System.Data;

/// <summary>
/// MySql控制器封装
/// </summary>
public class MySqlConnectorWrapper
{
    private string m_sConnectionString;

    public MySqlConnectorWrapper(string connectionString)
    {
        this.m_sConnectionString = connectionString;
    }

    /// <summary>
    /// 是否存在表
    /// </summary>
    /// <param name="tableName"></param>
    /// <returns></returns>
    public bool IsHasTable(string tableName)
    {
        string query = $"SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = '{tableName}';";
        using (MySqlConnection connection = new MySqlConnection(m_sConnectionString))
        {
            try
            {
                connection.Open();
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    int tableExists = Convert.ToInt32(command.ExecuteScalar());
                    return tableExists > 0;
                }
            }
            catch (Exception ex)
            {
                Debug.Instance.LogError($"MySqlConnectorWrapper IsHasTable {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// 执行查询并返回结果
    /// </summary>
    /// <param name="query"></param>
    /// <param name="parameters"></param>
    /// <returns></returns>
    public DataTable ExecuteQuery(string query)
    {
        DataTable dt = new DataTable();
        using (MySqlConnection connection = new MySqlConnection(m_sConnectionString))
        {
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                try
                {
                    connection.Open();
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(command))
                    {
                        adapter.Fill(dt);
                    }
                }
                catch (Exception ex)
                {
                    Debug.Instance.LogError($"MySqlConnectorWrapper ExecuteQuery {ex.Message}");
                }
            }
        }
        return dt;
    }

    /// <summary>
    /// 执行非查询操作
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    public int ExecuteNonQuery(string query)
    {
        int rowsAffected = 0;
        using (MySqlConnection connection = new MySqlConnection(m_sConnectionString))
        {
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                try
                {
                    connection.Open();
                    rowsAffected = command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Debug.Instance.LogError($"MySqlConnectorWrapper ExecuteNonQuery {ex.Message}");
                }
            }
        }
        return rowsAffected;
    }

    /// <summary>
    /// 插入数据
    /// </summary>
    /// <param name="tableName"></param>
    /// <param name="columnValues"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public int InsertData(string tableName, Dictionary<string, object> columnValues)
    {
        // 检查参数  
        if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException(nameof(tableName));
        if (columnValues == null || columnValues.Count == 0) throw new ArgumentException("没有提供列名和值", nameof(columnValues));

        // 构建列名和参数占位符  
        string columns = string.Join(", ", columnValues.Keys);
        string parameters = string.Join(", ", columnValues.Keys.Select(k => "@" + k));

        // 构建SQL语句  
        string query = $"INSERT INTO {tableName} ({columns}) VALUES ({parameters})";

        using (MySqlConnection connection = new MySqlConnection(m_sConnectionString))
        {
            try
            {
                connection.Open();
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    // 为每个列添加参数  
                    foreach (var kvp in columnValues)
                    {
                        command.Parameters.AddWithValue("@" + kvp.Key, kvp.Value);
                    }

                    // 执行命令并返回受影响的行数  
                    return command.ExecuteNonQuery();

                }
            }
            catch (Exception ex)
            {
                Debug.Instance.LogError($"MySqlConnectorWrapper InsertData {ex.Message}");
                return 0;
            }
        }
    }

    /// <summary>
    /// 更新数据
    /// </summary>
    /// <param name="tableName"></param>
    /// <param name="columnValues"></param>
    /// <param name="whereColumnName"></param>
    /// <param name="whereValue"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public int UpdateData(string tableName, Dictionary<string, object> columnValues, string whereColumnName, object whereValue)
    {
        // 检查参数  
        if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException(nameof(tableName));
        if (columnValues == null || columnValues.Count == 0) throw new ArgumentException("没有提供列名和值", nameof(columnValues));
        if (string.IsNullOrEmpty(whereColumnName)) throw new ArgumentNullException(nameof(whereColumnName));

        // 构建SET子句  
        string setClause = string.Join(", ", columnValues.Select(kvp => $"{kvp.Key} = @{kvp.Key}"));

        // 构建SQL语句  
        string query = $"UPDATE {tableName} SET {setClause} WHERE {whereColumnName} = @WhereValue";

        using (MySqlConnection connection = new MySqlConnection(m_sConnectionString))
        {
            try
            {
                connection.Open();
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    // 为每个列添加参数  
                    foreach (var kvp in columnValues)
                    {
                        command.Parameters.AddWithValue("@" + kvp.Key, kvp.Value);
                    }

                    // 添加WHERE子句的参数  
                    command.Parameters.AddWithValue("@WhereValue", whereValue);

                    // 执行命令并返回受影响的行数  
                    return command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Debug.Instance.LogError($"MySqlConnectorWrapper UpdateData {ex.Message}");
                return 0;
            }
        }
    }

    /// <summary>
    /// 删除数据
    /// </summary>
    /// <param name="tableName"></param>
    /// <param name="whereColumnName"></param>
    /// <param name="whereValue"></param>
    /// <returns></returns>
    public int DeleteData(string tableName, string whereColumnName, object whereValue)
    {
        string query = $"DELETE FROM {tableName} WHERE {whereColumnName} = @WhereValue";

        using (MySqlConnection connection = new MySqlConnection(m_sConnectionString))
        {
            try
            {
                connection.Open();
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@WhereValue", whereValue);
                    return command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Debug.Instance.LogError($"MySqlConnectorWrapper DeleteData {ex.Message}");
                return 0;
            }
        }
    }

    /// <summary>  
    /// 删除表中的所有数据  
    /// </summary>  
    /// <param name="tableName">要删除数据的表名</param>  
    /// <returns>影响的行数（理论上应该是表中的行数，但在某些情况下可能不同）</returns>  
    public int DeleteAllDataFromTable(string tableName)
    {
        string query = $"DELETE FROM {tableName}";

        using (MySqlConnection connection = new MySqlConnection(m_sConnectionString))
        {
            try
            {
                connection.Open();
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    return command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Debug.Instance.LogError($"MySqlConnectorWrapper DeleteAllDataFromTable {ex.Message}");
                return 0;
            }
        }
    }

    /// <summary>
    /// 查询数据
    /// </summary>
    /// <param name="tableName"></param>
    /// <param name="whereColumnName"></param>
    /// <param name="whereValue"></param>
    /// <returns></returns>
    public DataTable SelectData(string tableName, string whereColumnName = null, object whereValue = null)
    {
        string query = $"SELECT * FROM {tableName}";

        if (!string.IsNullOrEmpty(whereColumnName) && whereValue != null)
        {
            query += $" WHERE {whereColumnName} = @WhereValue";
        }

        DataTable dataTable = new DataTable();

        using (MySqlConnection connection = new MySqlConnection(m_sConnectionString))
        {
            try
            {
                connection.Open();
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    if (whereColumnName != null && whereValue != null)
                    {
                        command.Parameters.AddWithValue("@WhereValue", whereValue);
                    }

                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(command))
                    {
                        adapter.Fill(dataTable);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Instance.LogError($"MySqlConnectorWrapper SelectData {ex.Message}");
                return null;
            }
        }

        return dataTable;
    }

    /// <summary>  
    /// 使用多个键值对查询数据  
    /// </summary>  
    /// <param name="tableName">表名</param>  
    /// <param name="conditions">包含键值对的字典，作为查询条件</param>  
    /// <returns>查询结果DataTable</returns>  
    public DataTable SelectDataWithConditions(string tableName, Dictionary<string, object> conditions)
    {
        if (conditions == null || conditions.Count == 0)
        {
            throw new ArgumentException("Conditions dictionary cannot be null or empty.", nameof(conditions));
        }

        var queryBuilder = new System.Text.StringBuilder($"SELECT * FROM {tableName} WHERE 1=1");

        List<MySqlParameter> parameters = new List<MySqlParameter>();

        foreach (var kvp in conditions)
        {
            queryBuilder.Append($" AND {kvp.Key} = @{kvp.Key}");
            parameters.Add(new MySqlParameter($"@{kvp.Key}", kvp.Value));
        }

        string query = queryBuilder.ToString();

        DataTable dataTable = new DataTable();

        using (MySqlConnection connection = new MySqlConnection(m_sConnectionString))
        {
            try
            {
                connection.Open();
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddRange(parameters.ToArray());

                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(command))
                    {
                        adapter.Fill(dataTable);
                    }
                }
            }
            catch (Exception ex)
            {
                // 记录错误或处理异常  
                Debug.Instance.LogError($"MySqlConnectorWrapper SelectDataWithConditions: {ex.Message}");
                return null;
            }
        }
        return dataTable;
    }

    /// <summary>  
    /// 查询数据，并根据单个列进行排序。  
    /// </summary>  
    /// <param name="tableName">要查询的数据库表名。</param>  
    /// <param name="orderByColumnName">用于排序的列名。</param>  
    /// <param name="ascending">是否升序排序。</param>  
    /// <param name="whereColumnName">（可选）用于WHERE子句的列名。</param>  
    /// <param name="whereValue">（可选）与whereColumnName关联的值。</param>  
    /// <returns>包含查询结果的数据表。</returns> 
    public DataTable SelectDataWithOrder(string tableName, string orderByColumnName, bool ascending, string whereColumnName = null, object whereValue = null)
    {
        DataTable dataTable = new DataTable();
        using (MySqlConnection connection = new MySqlConnection(m_sConnectionString))
        {
            string query = $"SELECT * FROM {tableName}";
            if (!string.IsNullOrEmpty(whereColumnName) && whereValue != null)
            {
                query += $" WHERE {whereColumnName} = @whereValue";
            }
            query += $" ORDER BY {orderByColumnName} {(ascending ? "ASC" : "DESC")}";

            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                if (!string.IsNullOrEmpty(whereColumnName) && whereValue != null)
                {
                    command.Parameters.AddWithValue("@whereValue", whereValue);
                }

                try
                {
                    connection.Open();
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(command))
                    {
                        adapter.Fill(dataTable);
                    }
                }
                catch (Exception ex)
                {
                    Debug.Instance.LogError($"MySqlConnectorWrapper SelectDataWithOrder {ex.Message}");
                }
            }
        }

        return dataTable;
    }

    /// <summary>
    /// 查询数据，并根据两个列进行排序。  
    /// 如果第一个排序列的值相等，则根据第二个排序列进行排序。
    /// </summary>
    /// <param name="tableName">要查询的数据库表名。</param>  
    /// <param name="primaryOrderByColumnName">用于主要排序的列名。</param>  
    /// <param name="primaryAscending">主要排序是否升序排序。</param>  
    /// <param name="secondaryOrderByColumnName">用于次要排序的列名。</param>  
    /// <param name="secondaryAscending">次要排序是否升序排序。</param>  
    /// <param name="whereColumnName">（可选）用于WHERE子句的列名。</param>  
    /// <param name="whereValue">（可选）与whereColumnName关联的值。</param>  
    /// <returns>包含查询结果的数据表。</returns> 
    public DataTable SelectDataWithDualOrder(string tableName, string primaryOrderByColumnName, bool primaryAscending, string secondaryOrderByColumnName, bool secondaryAscending, string whereColumnName = null, object whereValue = null)
    {
        DataTable dataTable = new DataTable();
        using (MySqlConnection connection = new MySqlConnection(m_sConnectionString))
        {
            string query = $"SELECT * FROM {tableName}";
            if (!string.IsNullOrEmpty(whereColumnName) && whereValue != null)
            {
                query += $" WHERE {whereColumnName} = @whereValue";
            }
            query += $" ORDER BY {primaryOrderByColumnName} {(primaryAscending ? "ASC" : "DESC")}, {secondaryOrderByColumnName} {(secondaryAscending ? "ASC" : "DESC")}";

            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                if (!string.IsNullOrEmpty(whereColumnName) && whereValue != null)
                {
                    command.Parameters.AddWithValue("@whereValue", whereValue);
                }

                try
                {
                    connection.Open();
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(command))
                    {
                        adapter.Fill(dataTable);
                    }
                }
                catch (Exception ex)
                {
                    Debug.Instance.LogError($"MySqlConnectorWrapper SelectDataWithDualOrder {ex.Message}");
                }
            }
        }

        return dataTable;
    }

    /// <summary>
    /// 查询数据，并根据多个列进行排序。  
    /// </summary>
    /// <param name="tableName">要查询的数据库表名。</param>  
    /// <param name="orderByColumns">用于排序的列名和顺序的字典。</param>  
    /// <param name="whereColumnName">（可选）用于WHERE子句的列名。</param>  
    /// <param name="whereValue">（可选）与whereColumnName关联的值。</param>  
    /// <returns>包含查询结果的数据表。</returns>
    public DataTable SelectDataWithMultiOrder(string tableName, Dictionary<string, bool> orderByColumns, string whereColumnName = null, object whereValue = null)
    {
        DataTable dataTable = new DataTable();
        using (MySqlConnection connection = new MySqlConnection(m_sConnectionString))
        {
            string query = $"SELECT * FROM {tableName}";
            if (!string.IsNullOrEmpty(whereColumnName) && whereValue != null)
            {
                query += $" WHERE {whereColumnName} = @whereValue";
            }

            if (orderByColumns != null && orderByColumns.Count > 0)
            {
                var orderByClause = new List<string>();
                foreach (var column in orderByColumns)
                {
                    orderByClause.Add($"{column.Key} {(column.Value ? "ASC" : "DESC")}");
                }
                query += $" ORDER BY {string.Join(", ", orderByClause)}";
            }

            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                if (!string.IsNullOrEmpty(whereColumnName) && whereValue != null)
                {
                    command.Parameters.AddWithValue("@whereValue", whereValue);
                }

                try
                {
                    connection.Open();
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(command))
                    {
                        adapter.Fill(dataTable);
                    }
                }
                catch (Exception ex)
                {
                    Debug.Instance.LogError($"MySqlConnectorWrapper SelectDataWithMultiOrder {ex.Message}");
                }
            }
        }

        return dataTable;
    }

}