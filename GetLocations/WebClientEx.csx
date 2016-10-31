using System.Net;

//from: http://stackoverflow.com/questions/13039068/webclient-does-not-automatically-redirect
class WebClientEx : WebClient
{
  private CookieContainer _mContainer = new CookieContainer();

  protected override WebRequest GetWebRequest(Uri address)
  {
    var request = base.GetWebRequest(address);
    var webRequest = request as HttpWebRequest;
    if (webRequest != null)
    {
      webRequest.AllowAutoRedirect = false; //crucial to get 302 auth required redirect header, vs the 200 ok after redirect to signin
      webRequest.CookieContainer = _mContainer;
    }
    return request;
  }

  protected override WebResponse GetWebResponse(WebRequest request)
  {
    var response = base.GetWebResponse(request);
    var webResponse = response as HttpWebResponse;
    if (webResponse != null)
    {
      _mContainer.Add(webResponse.Cookies);
    }
    return response;
  }

  public void ClearCookies()
  {
    _mContainer = new CookieContainer();
  }
}