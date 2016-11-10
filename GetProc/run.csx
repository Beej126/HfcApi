#r "SqlClientHelpers.dll"
#r "System.Data"
using System.Net;
using System;
using System.Linq;
using System.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SqlClientHelpers;

public static string Env(string name) => System.Environment.GetEnvironmentVariable(name /*, EnvironmentVariableTarget.Process*/);

public static JArray DataTableToJson(this DataTableCollection tables)
{
  var tablesArray = new JArray();

  foreach (DataTable t in tables)
  {
    JArray rowsArray = new JArray();
    JObject row;
    foreach (DataRow dr in t.Rows)
    {
      row = new JObject();
      foreach (DataColumn col in t.Columns)
      {
        row.Add(col.ColumnName.Trim(), JToken.FromObject(dr[col]));
      }
      rowsArray.Add(row);
    }
    tablesArray.Add(rowsArray);
  }
  return tablesArray;
}

private static TraceWriter traceLog;
public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    traceLog = log;

    traceLog.Info("[Run]");
  /*
  log.Info($"C# HTTP trigger function processed a request. RequestUri={req.RequestUri}");

  // parse query parameter
  string name = req.GetQueryNameValuePairs()
      .FirstOrDefault(q => string.Compare(q.Key, "name", true) == 0)
      .Value;

  // Get request body
  dynamic data = await req.Content.ReadAsAsync<object>();

  // Set name to query string or body data
  name = name ?? data?.name;

  return name == null
      ? req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a name on the query string or in the request body")
      : req.CreateResponse(HttpStatusCode.OK, "Hello " + name);
  */

  System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string>>parms = req.GetQueryNameValuePairs();
  traceLog.Info($"parms.Count(): {parms.Count()}, first: {parms.First().Value}");

  Proc.DefaultConnectionString = $"User ID={Env("sqluser")};Password={Env("sqlpassword")};Initial Catalog=SlingshotAX;Data Source={Env("sqlserver")};";
  SqlClientHelpers.Proc.MessageCallback = (msg) => { }; //this suppresses query option hint warning messages turning into exceptions
  traceLog.Info($"DefaultConnectionString: {Proc.DefaultConnectionString}");

  string result = "no data";
  using (var proc = new Proc($"dbo.ProdDetail_mobile")) {  //{parms.Get("procName")}
    var sqlparms = proc.Parameters;
    foreach(var nv in parms) { 
      var key = "@"+nv.Key;
      traceLog.Info($"name: {key}, value: {nv.Value}");
      proc[key] = nv.Value;
    }
    result = DataTableToJson(proc.ExecuteDataSet().Tables).ToString();
  }
    return req.CreateResponse(HttpStatusCode.OK, 
      result,
      System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);

}
