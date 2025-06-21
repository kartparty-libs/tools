
/// <summary>
/// 地图网格
/// </summary>
public class MapGrid
{
    /// <summary>
    /// 网格列表
    /// </summary>
    private List<HashSet<long>> m_tMapGrids = new List<HashSet<long>>();

    /// <summary>
    /// 实例Id位置映射
    /// </summary>
    private Dictionary<long, int> m_pInstIdMapping = new Dictionary<long, int>();

    private int m_nMaxX = 0;
    private int m_nMaxY = 0;
    private int m_nSizeX = 0;
    private int m_nSizeY = 0;
    private int m_nGridX = 0;
    private int m_nGridY = 0;

    public MapGrid()
    {
        int[] mapSize = ServerConfig.GetToIntArray("map_size");
        int[] mapGrid = ServerConfig.GetToIntArray("map_grid");
        m_nSizeX = mapSize[0];
        m_nSizeY = mapSize[1];
        m_nGridX = mapGrid[0];
        m_nGridY = mapGrid[1];
        m_nMaxX = m_nSizeX / m_nGridX;
        m_nMaxY = m_nSizeY / m_nGridY;
        int count = m_nMaxX * m_nMaxY;
        for (int i = 0; i < count; i++)
        {
            m_tMapGrids.Add(new HashSet<long>());
        }
    }

    public List<long> Add(int x, int y, long instId)
    {
        int gx = x / m_nGridX;
        int gy = y / m_nGridY;
        int i = gx + gy * m_nMaxX;

        if (m_pInstIdMapping.TryGetValue(instId, out int oldIndex))
        {
            if (oldIndex == i)
            {
                return Get(instId);
            }
            else
            {
                m_tMapGrids[oldIndex].Remove(instId);
                m_pInstIdMapping.Remove(instId);
            }
        }

        if (m_tMapGrids.Count < i)
        {
            return null;
        }

        m_tMapGrids[i].Add(instId);
        m_pInstIdMapping.Add(instId, i);
        return Get(instId);
    }

    public void Remove(long instId)
    {
        if (m_pInstIdMapping.TryGetValue(instId, out int oldIndex))
        {
            m_tMapGrids[oldIndex].Remove(instId);
            m_pInstIdMapping.Remove(instId);
        }
    }

    public List<long> Get(long instId)
    {
        if (m_pInstIdMapping.TryGetValue(instId, out int oldIndex))
        {
            return GetGridPlayerList(oldIndex);
        }
        return null;
    }

    public int GetGridIdByInstId(long instId)
    {
        if (m_pInstIdMapping.TryGetValue(instId, out int oldIndex))
        {
            return oldIndex;
        }
        return -1;
    }

    public List<long> GetGridPlayerList(int gridId)
    {
        List<long> instIds = new List<long>();
        int index0 = gridId;
        int index1 = gridId - 1 - m_nMaxY;
        int index2 = gridId + 1 + m_nMaxY;
        int index3 = gridId - 1 + m_nMaxY;
        int index4 = gridId + 1 - m_nMaxY;
        int index5 = gridId - 1;
        int index6 = gridId + 1;
        int index7 = gridId - m_nMaxY;
        int index8 = gridId + m_nMaxY;
        List<int> indexs = new List<int>
            {
                index0, index1, index2, index3, index4, index5, index6, index7, index8,
            };
        for (int i = 0; i < indexs.Count; i++)
        {
            if (indexs[i] >= 0 && m_tMapGrids.Count > i)
            {
                instIds.AddRange(m_tMapGrids[indexs[i]].ToList());
            }
        }

        return instIds;
    }
}