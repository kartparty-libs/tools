
using Proto;

public class ResJsonData
{
    public static readonly string code = nameof(code);
    public static readonly string date = nameof(date);
    public static readonly string result = nameof(result);

    private Dictionary<string, object> i_pResponseData = new Dictionary<string, object>();

    public ResJsonData()
    {
        i_pResponseData.Add(code, ResCodeEnum.UnknownError);
        i_pResponseData.Add(date, UtilityMethod.GetUnixTimeMilliseconds());
        i_pResponseData.Add(result, new Dictionary<string, object>());
    }

    public ResCodeEnum GetCode()
    {
        return (ResCodeEnum)i_pResponseData[code];
    }

    public void SetCode(ResCodeEnum i_eResCodeEnum)
    {
        i_pResponseData[code] = i_eResCodeEnum;
    }

    public long GetDate()
    {
        return (long)i_pResponseData[date];
    }

    public void SetDate(long i_nDate)
    {
        i_pResponseData[date] = i_nDate;
    }

    public Dictionary<string, object> GetResponseResult()
    {
        return i_pResponseData[result] as Dictionary<string, object>;
    }

    public object GetResponseResultData(string i_sKey)
    {
        if (i_pResponseData[result] is Dictionary<string, object> _responseData)
        {
            return _responseData[i_sKey];
        }
        return null;
    }

    public void AddResponseResultData(string i_sKey, object i_pValue)
    {
        if (i_pResponseData[result] is Dictionary<string, object> _responseData)
        {
            if (!_responseData.ContainsKey(i_sKey))
            {
                _responseData.Add(i_sKey, i_pValue);
            }
            else
            {
                _responseData[i_sKey] = i_pValue;
            }
        }
        SetDate(UtilityMethod.GetUnixTimeMilliseconds());
    }

    public string GetResponseJsonData()
    {
        return UtilityMethod.JsonSerializeObject(i_pResponseData);
    }
}
