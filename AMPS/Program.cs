using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

string url = "https://shlefu.candypay.com/AMPS/unifiedPay/gateway";
string mchtId = "";
string devId = "";
string secret = "";

app.MapGet("/ok", () =>
{
    return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "ok";
});

app.MapGet("/testpay", async () =>
{
    PayParamsModel model = new PayParamsModel()
    {
        body = "测试商品描述",
        date_time = DateTime.Now.ToString("yyyyMMddHHmmss"),
        dev_id = devId,
        mcht_id = mchtId,
        mch_create_ip= "127.0.0.1",
        nonce_str="pyOF58ElztA7a9XFlWprodXsjSDhcVAd",
        open_id="yOF58ElztA7a9XFlWprodXsjS",
        out_order_no = "O123456677",
        total_fee = 10,
        txn_num = "unified_js_pay",
    };
    string sign = getSignStr(model);
    model.sign = sign;

    HttpContent content = new StringContent(System.Text.Json.JsonSerializer.Serialize(model));
    HttpClient client = new HttpClient();
    var rsp = await client.PostAsync(url, content);
    var rspStr = await rsp.Content.ReadAsStringAsync();
    return rspStr;
})
.WithName("pay")
.WithOpenApi();

app.Run();

string getSignStr(PayParamsModel model)
{
    Type type = typeof(PayParamsModel);
    SortedDictionary<string,string> pairs = new SortedDictionary<string, string>();
    foreach (var item in type.GetProperties())
    {
        var name = item.Name;
        if (name == "sign")
        {
            continue;
        }
        var value = item.GetValue(model);
        if (value == null)
        {
            continue;
        }
        pairs.Add(name,value!.ToString());
    } 

    StringBuilder sb = new StringBuilder();
    foreach (var item in pairs)
    {
        sb.Append(item.Key+"="+item.Value+"&");
    }
    string beforeMd5 = sb.ToString().TrimEnd('&') + secret;

    return MD5Encrypt32(beforeMd5).ToUpper();
}
string MD5Encrypt32(string input)
{
    string cl = input;
    string result = "";
    System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
    byte[] s = md5.ComputeHash(Encoding.UTF8.GetBytes(cl));
    for (int i = 0; i < s.Length; i++)
    {
        result = result + s[i].ToString("X");
    }
    return result;
}
class PayParamsModel
{
    public string txn_num { get; set; }
    public string version { get; set; } = "3.0";
    public string date_time { get; set; }
    public string dev_id { get; set; }
    public string mcht_id { get; set; }
    public string out_order_no { get; set; }

    public string sign { get; set; }
    public string nonce_str { get; set; }

    /// <summary>
    /// 总金额
    /// </summary>
    public int total_fee { get; set; }
    /// <summary>
    /// 商品描述
    /// </summary>
    public string body { get; set; }
    /// <summary>
    /// 回调地址
    /// </summary>
    public string notify_url { get; set; }
    /// <summary>
    /// 订单生成的机器ip
    /// </summary>
    public string mch_create_ip { get; set; }
    /// <summary>
    /// 微信支付必填
    /// </summary>
    public string? open_id { get; set; }
    /// <summary>
    /// 支付宝支付必填，买家支付宝用户id
    /// </summary>
    public string? buyer_id { get; set; }
    /// <summary>
    /// 子商户公众账号id
    /// </summary>
    public string? sub_appid { get; set; }

}