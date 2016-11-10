#load "WebClientEx.csx" //custom WebClient that tracks cookies for Session/login support

using System.Net;
using System;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class Device {
  public int Id { get; set; }
  public string Name { get; set; }
  public double? Lat { get; set; }
  public double? Lng { get; set; }
  public string Status_Text {get; set; }
  public int Icon_Id {get; set;}
  public int? Group_Id {get; set;}
  public string GroupName { get; set; }
}

public class Group {
  public int Id { get; set; }
  public string Name { get; set; }
  public int Division_Id {get; set;}
}

public static string Env(string name) => System.Environment.GetEnvironmentVariable(name /*, EnvironmentVariableTarget.Process*/);
private static string _baseUrl;
private static string baseUrl {get { return _baseUrl ?? (_baseUrl = Env("gpsBaseUrl")); }}

private static void login() {
    traceLog.Info($"[login] baseUrl: {baseUrl}, email: {Env("email")}");

    //login - this registers their _session_id cookie in the customized WebClient instance above...
    //  *** so that subsequent requests for data are authorized ***
    //login is pretty slow so we'll want to cache this state in our API layer
    var fields = new NameValueCollection();
    fields.Add("user[email]", Env("gpsEmail"));
    fields.Add("user[password]", Env("gpsPassword"));
    fields.Add("subdomain", Env("gpsSubdomain"));
    client.UploadValues($"{baseUrl}/users/sign_in", fields);
}

private static string Request(Func<string> func) {
    var result = func();
    if (client.ResponseHeaders["location"].EndsWith("sign_in")) login();
    return func();
}

private static List<Device> devices;
private static string devicesQueryString;
private static Dictionary<int, Group> groupsDict;
private static void InitCodeTables() {
    login();

    traceLog.Info("[InitCodeTables]");
    //i haven't yet found an clean REST API to return the initial list of devices... 
    //currently "scraping" this from html that has inline JS populating the lists of objects we need...
    //unfortunately this full page hit is significantly slower than a simple REST API data grab...
    //so we'll want to cache this is API layer as much as possible as well
    var resultCodeTables = client.DownloadString($"{baseUrl}");
    //via RegEx filtering, pull the basic object lists (devices, groups aka divisions) out of inline <script> in raw html page
    var deviceMatches = Regex.Matches(resultCodeTables, @"devices.push\(({id.*?})").Cast<Match>().Select(m => m.Groups[1].Value);
    var groupMatches = Regex.Matches(resultCodeTables, @"groups.push\(({id.*?})").Cast<Match>().Select(m => m.Groups[1].Value);
    //concat individual lines of jscript into json strings
    var devicesJson = $"[{String.Join(",", deviceMatches)}]";
    var groupsJson = $"[{String.Join(",", groupMatches)}]";
    //parse the json into full fidelity C# object collections
    devices = JsonConvert.DeserializeObject<List<Device>>(devicesJson);
    traceLog.Info($"devices count: {devices.Count}");

    //build query string parms for requesting GPS for each device...
    devicesQueryString = string.Join("", devices.Select(d => d.Id.ToString($"&device[]={0}")));

    var groupsDict = JArray.Parse(groupsJson).ToDictionary( i=>i["id"].Value<int>(), i=>i.ToObject<Group>() );
    //create a group entry to represent devices with no group currently assigned
    groupsDict.Add(0, new Group {Id = 0, Name = "{unassigned}", Division_Id = 0});
    traceLog.Info($"devices count: {groupsDict.Count}");

    foreach(var d in devices) {
        d.GroupName = groupsDict[d.Group_Id ?? 0].Name; //slap the GroupName directly on to each device to avoid recurring lookups 
    }
}


private static WebClient client; //this caches login state
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

    if (client == null) client = new WebClientEx();
    
    if (devicesQueryString == null) InitCodeTables(); 

    //fire this more traditional REST API request for GPS data on each device (used by their web front end)... 
    //this returns clean json and is a very quick hit so we can frequently refresh
    //TBD: change over to "home/minimal_map_refresh?section=index&counter=0&last_refresh=1477677900"
    traceLog.Info("[1]");
    var deviceGpsJson =client.UploadString($"{baseUrl}/mydash/get_location?widget=365821{devicesQueryString}","");
    traceLog.Info("[1.1]");
    var redirect = client.ResponseHeaders["location"];
    if (redirect != null && redirect.EndsWith("sign_in")) login();
    traceLog.Info("[1.2]");
    deviceGpsJson = client.UploadString($"{baseUrl}/mydash/get_location?widget=365821{devicesQueryString}","");

    traceLog.Info("[2]");
    var devicesGpsDict = JObject.Parse(deviceGpsJson)["locations"].ToDictionary( i=>i["id"].Value<int>(), i=>i.ToObject<Device>() );
    traceLog.Info("[3]");

    //merge the gps values into the original list (since the gps results don't come back with all previously obtained device properties)
    foreach(var d in devices) {
        d.Lat = devicesGpsDict[d.Id].Lat;
        d.Lng = devicesGpsDict[d.Id].Lng;
    }

    return req.CreateResponse(HttpStatusCode.OK, JsonConvert.SerializeObject(devices), System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
}
