using System.Collections.Concurrent;
using System.Data;

public class DBServer
{
    public static bool Pooling = true;
    public static int MinImumPoolSize = 20;
    public static int MaxImumPoolSize = 100;

    private ConcurrentDictionary<ServerTypeEnum, MySqlConnectorWrapper> m_pMySqlConnectorWrappers = new ConcurrentDictionary<ServerTypeEnum, MySqlConnectorWrapper>();
    private Dictionary<ServerTypeEnum, bool> m_pServerIsNews = new Dictionary<ServerTypeEnum, bool>();

    public void Initializer()
    {
        foreach (var item in ServerConfig.ServerOpens)
        {
            if (ServerConfig.GetToString("db_host", item.Key) != null)
            {
                AddMySqlConnector(item.Key, ServerConfig.GetToString("db_host", item.Key), ServerConfig.GetToInt("db_port", item.Key), ServerConfig.GetToString("db_name", item.Key), ServerConfig.GetToString("db_user", item.Key), ServerConfig.GetToString("db_pwd", item.Key), ServerConfig.GetToString("db_sql", item.Key));
            }
        }
    }

    public void Close()
    {
    }

    // --------------------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// 添加MySql控制器
    /// </summary>
    /// <param name="i_eServerTypeEnum"></param>
    /// <param name="i_sHost"></param>
    /// <param name="i_nPort"></param>
    /// <param name="i_sDataBase"></param>
    /// <param name="i_sUser"></param>
    /// <param name="i_sPwd"></param>
    /// <param name="i_sSqlName"></param>
    public void AddMySqlConnector(ServerTypeEnum i_eServerTypeEnum, string i_sHost, int i_nPort, string i_sDataBase, string i_sUser, string i_sPwd, string i_sSqlName)
    {
        if (m_pMySqlConnectorWrappers.ContainsKey(i_eServerTypeEnum))
        {
            return;
        }
        MySqlConnectorWrapper mySqlConnectorWrapper = CreateMySqlConnector(i_eServerTypeEnum, i_sHost, i_nPort, i_sDataBase, i_sUser, i_sPwd, i_sSqlName);
        m_pMySqlConnectorWrappers.TryAdd(i_eServerTypeEnum, mySqlConnectorWrapper);
    }

    /// <summary>
    /// 创建MySql控制器
    /// </summary>
    /// <param name="i_eServerTypeEnum"></param>
    /// <param name="i_sHost"></param>
    /// <param name="i_nPort"></param>
    /// <param name="i_sDataBase"></param>
    /// <param name="i_sUser"></param>
    /// <param name="i_sPwd"></param>
    /// <param name="i_sSqlName"></param>
    /// <returns></returns>
    private MySqlConnectorWrapper CreateMySqlConnector(ServerTypeEnum i_eServerTypeEnum, string i_sHost, int i_nPort, string i_sDataBase, string i_sUser, string i_sPwd, string i_sSqlName)
    {
        Debug.Instance.LogInfo($"DBServer Initializer {i_sDataBase} Start");
        // 配置连接池
        string gamedbString = $"Host={i_sHost};Port={i_nPort};Database={i_sDataBase};User={i_sUser};Pwd={i_sPwd};";
        string connectionSettingString = $"Pooling={Pooling.ToString()};Minimum Pool Size={MinImumPoolSize};Maximum Pool Size={MaxImumPoolSize};";
        string connectionString = gamedbString + connectionSettingString;
        Debug.Instance.LogInfo($"DBServer Create SQL {gamedbString}");
        MySqlConnectorWrapper mySqlConnectorWrapper = new MySqlConnectorWrapper(connectionString);

        bool isNew = false;
        // 首次创建数据库
        if (!mySqlConnectorWrapper.IsHasTable(SqlTableName.globalinfo))
        {
            isNew = true;
        }
        else
        {
            DataTable globalinfo = mySqlConnectorWrapper.SelectData(SqlTableName.globalinfo, "serverid", ServerConfig.GetToInt("serverid"));
            if (globalinfo == null || globalinfo.Rows.Count == 0)
            {
                isNew = true;
            }
        }
        string sqlCreateConfigPath = Path.Combine("config", "sql", $"{i_sSqlName}"); ;
        string sqlCreateConfig = File.ReadAllText(sqlCreateConfigPath);
        mySqlConnectorWrapper.ExecuteNonQuery(sqlCreateConfig);
        Debug.Instance.LogInfo($"DBServer Create SQL {sqlCreateConfigPath}");

        if (isNew)
        {
            Dictionary<string, object> columnValues = new Dictionary<string, object>();
            columnValues.Add("serverid", ServerConfig.GetToInt("serverid"));
            columnValues.Add("opentime", UtilityMethod.GetUnixTimeMilliseconds());
            mySqlConnectorWrapper.InsertData(SqlTableName.globalinfo, columnValues);
        }

        if (!m_pServerIsNews.ContainsKey(i_eServerTypeEnum))
        {
            m_pServerIsNews.Add(i_eServerTypeEnum, isNew);
        }

        Debug.Instance.LogInfo($"DBServer Initializer {i_sDataBase} Succeed");
        return mySqlConnectorWrapper;
    }

    /// <summary>
    /// 是否是新服
    /// </summary>
    /// <param name="i_eServerTypeEnum"></param>
    /// <returns></returns>
    public bool IsNewServer(ServerTypeEnum i_eServerTypeEnum)
    {
        if (m_pServerIsNews.TryGetValue(i_eServerTypeEnum, out bool isNew))
        {
            return isNew;
        }
        return false;
    }
    // --------------------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// 是否存在表
    /// </summary>
    /// <param name="serverTypeEnum"></param>
    /// <param name="tableName"></param>
    /// <returns></returns>
    public bool IsHasTable(ServerTypeEnum serverTypeEnum, string tableName)
    {
        if (m_pMySqlConnectorWrappers.TryGetValue(serverTypeEnum, out MySqlConnectorWrapper mySqlConnectorWrapper))
        {
            return mySqlConnectorWrapper.IsHasTable(tableName);
        }
        return false;
    }

    /// <summary>
    /// 执行查询并返回结果
    /// </summary>
    /// <param name="serverTypeEnum"></param>
    /// <param name="query"></param>
    /// <param name="parameters"></param>
    /// <returns></returns>
    public DataTable ExecuteQuery(ServerTypeEnum serverTypeEnum, string query)
    {
        if (m_pMySqlConnectorWrappers.TryGetValue(serverTypeEnum, out MySqlConnectorWrapper mySqlConnectorWrapper))
        {
            return mySqlConnectorWrapper.ExecuteQuery(query);
        }
        return default;
    }

    /// <summary>
    /// 执行非查询操作
    /// </summary>
    /// <param name="serverTypeEnum"></param>
    /// <param name="query"></param>
    /// <returns></returns>
    public int ExecuteNonQuery(ServerTypeEnum serverTypeEnum, string query)
    {
        if (m_pMySqlConnectorWrappers.TryGetValue(serverTypeEnum, out MySqlConnectorWrapper mySqlConnectorWrapper))
        {
            return mySqlConnectorWrapper.ExecuteNonQuery(query);
        }
        return default;
    }

    /// <summary>
    /// 插入数据
    /// </summary>
    /// <param name="serverTypeEnum"></param>
    /// <param name="tableName"></param>
    /// <param name="columnValues"></param>
    /// <returns></returns>
    public int InsertData(ServerTypeEnum serverTypeEnum, string tableName, Dictionary<string, object> columnValues)
    {
        if (m_pMySqlConnectorWrappers.TryGetValue(serverTypeEnum, out MySqlConnectorWrapper mySqlConnectorWrapper))
        {
            return mySqlConnectorWrapper.InsertData(tableName, columnValues);
        }
        return default;
    }

    /// <summary>
    /// 更新数据
    /// </summary>
    /// <param name="serverTypeEnum"></param>
    /// <param name="tableName"></param>
    /// <param name="columnValues"></param>
    /// <param name="whereColumnName"></param>
    /// <param name="whereValue"></param>
    /// <returns></returns>
    public int UpdateData(ServerTypeEnum serverTypeEnum, string tableName, Dictionary<string, object> columnValues, string whereColumnName, object whereValue)
    {
        if (m_pMySqlConnectorWrappers.TryGetValue(serverTypeEnum, out MySqlConnectorWrapper mySqlConnectorWrapper))
        {
            return mySqlConnectorWrapper.UpdateData(tableName, columnValues, whereColumnName, whereValue);
        }
        return default;
    }

    /// <summary>
    /// 删除数据
    /// </summary>
    /// <param name="serverTypeEnum"></param>
    /// <param name="tableName"></param>
    /// <param name="whereColumnName"></param>
    /// <param name="whereValue"></param>
    /// <returns></returns>
    public int DeleteData(ServerTypeEnum serverTypeEnum, string tableName, string whereColumnName, object whereValue)
    {
        if (m_pMySqlConnectorWrappers.TryGetValue(serverTypeEnum, out MySqlConnectorWrapper mySqlConnectorWrapper))
        {
            return mySqlConnectorWrapper.DeleteData(tableName, whereColumnName, whereValue);
        }
        return default;
    }

    /// <summary>  
    /// 删除表中的所有数据  
    /// </summary>  
    /// <param name="serverTypeEnum"></param>
    /// <param name="tableName">要删除数据的表名</param>  
    /// <returns>影响的行数（理论上应该是表中的行数，但在某些情况下可能不同）</returns>  
    public int DeleteAllDataFromTable(ServerTypeEnum serverTypeEnum, string tableName)
    {
        if (m_pMySqlConnectorWrappers.TryGetValue(serverTypeEnum, out MySqlConnectorWrapper mySqlConnectorWrapper))
        {
            return mySqlConnectorWrapper.DeleteAllDataFromTable(tableName);
        }
        return default;
    }

    /// <summary>
    /// 查询数据
    /// </summary>
    /// <param name="serverTypeEnum"></param>
    /// <param name="tableName"></param>
    /// <param name="whereColumnName"></param>
    /// <param name="whereValue"></param>
    /// <returns></returns>
    public DataTable SelectData(ServerTypeEnum serverTypeEnum, string tableName, string whereColumnName = null, object whereValue = null)
    {
        if (m_pMySqlConnectorWrappers.TryGetValue(serverTypeEnum, out MySqlConnectorWrapper mySqlConnectorWrapper))
        {
            return mySqlConnectorWrapper.SelectData(tableName, whereColumnName, whereValue);
        }
        return default;
    }

    /// <summary>  
    /// 使用多个键值对查询数据  
    /// </summary>  
    /// <param name="tableName">表名</param>  
    /// <param name="conditions">包含键值对的字典，作为查询条件</param>  
    /// <returns>查询结果DataTable</returns>  
    public DataTable SelectDataWithConditions(ServerTypeEnum serverTypeEnum, string tableName, Dictionary<string, object> conditions)
    {
        if (m_pMySqlConnectorWrappers.TryGetValue(serverTypeEnum, out MySqlConnectorWrapper mySqlConnectorWrapper))
        {
            return mySqlConnectorWrapper.SelectDataWithConditions(tableName, conditions);
        }
        return default;
    }

    /// <summary>  
    /// 查询数据，并根据单个列进行排序。  
    /// </summary>  
    /// <param name="serverTypeEnum"></param>  
    /// <param name="tableName">要查询的数据库表名。</param>  
    /// <param name="orderByColumnName">用于排序的列名。</param>  
    /// <param name="ascending">是否升序排序。</param>  
    /// <param name="whereColumnName">（可选）用于WHERE子句的列名。</param>  
    /// <param name="whereValue">（可选）与whereColumnName关联的值。</param>  
    /// <returns>包含查询结果的数据表。</returns> 
    public DataTable SelectDataWithOrder(ServerTypeEnum serverTypeEnum, string tableName, string orderByColumnName, bool ascending, string whereColumnName = null, object whereValue = null)
    {
        if (m_pMySqlConnectorWrappers.TryGetValue(serverTypeEnum, out MySqlConnectorWrapper mySqlConnectorWrapper))
        {
            return mySqlConnectorWrapper.SelectDataWithOrder(tableName, orderByColumnName, ascending, whereColumnName, whereValue);
        }
        return default;
    }

    /// <summary>
    /// 查询数据，并根据两个列进行排序。  
    /// 如果第一个排序列的值相等，则根据第二个排序列进行排序。
    /// </summary>
    /// <param name="serverTypeEnum"></param>  
    /// <param name="tableName">要查询的数据库表名。</param>  
    /// <param name="primaryOrderByColumnName">用于主要排序的列名。</param>  
    /// <param name="primaryAscending">主要排序是否升序排序。</param>  
    /// <param name="secondaryOrderByColumnName">用于次要排序的列名。</param>  
    /// <param name="secondaryAscending">次要排序是否升序排序。</param>  
    /// <param name="whereColumnName">（可选）用于WHERE子句的列名。</param>  
    /// <param name="whereValue">（可选）与whereColumnName关联的值。</param>  
    /// <returns>包含查询结果的数据表。</returns> 
    public DataTable SelectDataWithDualOrder(ServerTypeEnum serverTypeEnum, string tableName, string primaryOrderByColumnName, bool primaryAscending, string secondaryOrderByColumnName, bool secondaryAscending, string whereColumnName = null, object whereValue = null)
    {
        if (m_pMySqlConnectorWrappers.TryGetValue(serverTypeEnum, out MySqlConnectorWrapper mySqlConnectorWrapper))
        {
            return mySqlConnectorWrapper.SelectDataWithDualOrder(tableName, primaryOrderByColumnName, primaryAscending, secondaryOrderByColumnName, secondaryAscending, whereColumnName, whereValue);
        }
        return default;
    }

    /// <summary>
    /// 查询数据，并根据多个列进行排序。  
    /// </summary>
    /// <param name="tableName">要查询的数据库表名。</param>  
    /// <param name="orderByColumns">用于排序的列名和顺序的字典。</param>  
    /// <param name="whereColumnName">（可选）用于WHERE子句的列名。</param>  
    /// <param name="whereValue">（可选）与whereColumnName关联的值。</param>  
    /// <returns>包含查询结果的数据表。</returns>
    public DataTable SelectDataWithMultiOrder(ServerTypeEnum serverTypeEnum, string tableName, Dictionary<string, bool> orderByColumns, string whereColumnName = null, object whereValue = null)
    {
        if (m_pMySqlConnectorWrappers.TryGetValue(serverTypeEnum, out MySqlConnectorWrapper mySqlConnectorWrapper))
        {
            return mySqlConnectorWrapper.SelectDataWithMultiOrder(tableName, orderByColumns, whereColumnName, whereValue);
        }
        return default;
    }
}