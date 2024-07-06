using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;

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

app.MapGet("/testpay", async () =>
{
    PayParamsModel model = new PayParamsModel()
    {
        body = "测试商品描述",
        date_time = DateTime.Now.ToString("yyyyMMddHHmmss"),
        dev_id = devId,
        mcht_id = mchtId,
        mch_create_ip = "127.0.0.1",
        nonce_str = "pyOF58ElztA7a9XFlWprodXsjSDhcVAd",
        // open_id = "yOF58ElztA7a9XFlWprodXsjS",
        buyer_id = "yOF58ElztA7a9XFlWprodXsjS",
        out_order_no = "O123456678",
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

// app.MapPost("/test", async ([FromBody] Object req) =>
// {
//     await File.AppendAllTextAsync("test.txt", DateTime.Now.ToLongTimeString() + System.Text.Json.JsonSerializer.Serialize(req) + Environment.NewLine);
//     return "success";
// });

app.MapPost("/notify", async ([FromBody] NotifyRequestModel req) =>
{
    await File.AppendAllTextAsync("notifyContent.txt", DateTime.Now.ToLongTimeString() + System.Text.Json.JsonSerializer.Serialize(req) + Environment.NewLine);
    return "success";
});

app.Run();

string getSignStr(PayParamsModel model)
{
    Type type = typeof(PayParamsModel);
    SortedDictionary<string, string> pairs = new SortedDictionary<string, string>();
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
        pairs.Add(name, value!.ToString());
    }

    StringBuilder sb = new StringBuilder();
    foreach (var item in pairs)
    {
        sb.Append(item.Key + "=" + item.Value + "&");
    }
    string beforeMd5 = sb.ToString() + secret;
    var md5 = MD5Encrypt32(beforeMd5).ToUpper();
    return md5;
}
string MD5Encrypt32(string input)
{
    using (MD5 md5 = MD5.Create())
    {
        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        byte[] hashBytes = md5.ComputeHash(inputBytes);

        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < hashBytes.Length; i++)
        {
            sb.Append(hashBytes[i].ToString("X2"));
        }
        return sb.ToString().ToUpper();
    }
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

class NotifyRequestModel
{
    /// <summary>
    /// 用户标识
    /// </summary>
    public string open_id { get; set; }
    /// <summary>
    /// 子商户公众账号ID
    /// </summary>
    public string? sub_appid { get; set; }
    /// <summary>
    /// 子商户公众号用户的用户标识
    /// </summary>
    public string? sub_openid { get; set; }
    /// <summary>
    /// 是否关注公众账号
    /// </summary>
    public string? is_subscribe { get; set; }
    /// <summary>
    /// 买家支付宝账号
    /// </summary>
    public string? buyer_logon_id { get; set; }
    /// <summary>
    /// 买家支付宝用户ID
    /// </summary>
    public string? buyer_id { get; set; }
    /// <summary>
    /// 交易状态 1成功2失败
    /// </summary>
    public string org_txn_state { get; set; }
    /// <summary>
    /// 支付模式
    /// 1 – 微信刷卡支付
    /// 2 – 微信扫码支付
    /// 3 – 微信 app 支付
    /// 4 – 微信公众号支付
    /// 5 – 支付宝刷卡支付
    /// 6 – 支付宝扫码支付
    /// 7 – 支付宝 app 支付
    /// 8 – 支付宝服务窗支付
    /// 20 - 微信小程序支付
    /// 21 – 银联二维码 JS 支付
    /// 22 – 银联二维码刷卡支付
    /// 23 – 银联二维码扫码支付
    /// </summary>
    public string org_pay_mode { get; set; }
    /// <summary>
    /// 付款银行
    /// </summary>
    public string? bank_type { get; set; }
    /// <summary>
    /// 总金额 以分为单位
    /// </summary>
    public int total_fee { get; set; }
    /// <summary>
    /// 应结订单金额
    /// </summary>
    public int? settlement_total_fee { get; set; }
    /// <summary>
    /// 货币类型
    /// </summary>
    public string? fee_type { get; set; }
    /// <summary>
    /// 现金支付金额
    /// </summary>
    public int? cash_fee { get; set; }
    /// <summary>
    /// 现金支付货币类型
    /// </summary>
    public string? cash_fee_type { get; set; }
    /// <summary>
    /// 平台订单号
    /// </summary>
    public string? transaction_id { get; set; }
    /// <summary>
    /// 支付完成时间
    /// </summary>
    public string? time_end { get; set; }
    /// <summary>
    /// 第三方订单号 第三方交易号,如微信、支付宝交易单号
    /// </summary>
    public string? out_transaction_id { get; set; }
    /// <summary>
    /// 第三方商户订单号  第三方商户单号,可在支持的商户扫码退款
    /// </summary>
    public string? third_order_no { get; set; }

}