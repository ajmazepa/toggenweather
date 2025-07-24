using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Net.Mail;
using WeatherGetter;

//Definitions
string reportHTML = ""; //Weather report formatted as HTML
string reportSMS = ""; //Weather report formatted for SMS
string weatherURL = ConfigurationManager.AppSettings["weatherURL"]; //Get forecast URL from App.config
int daysAhead = 0; //Default to pulling today's weather
if (args != null) //Allow command line parameter to specify forecast for tomorrow
{
    foreach (var arg in args)
    {
        if (arg.ToLower() == "tomorrow")
        {
            daysAhead = 1;
        }
    }
}
string dateMatcher = DateTime.Now.AddDays(daysAhead).ToString("ddd MMM d"); //Generate date string for matching with TWN
List<string> emails = new List<string>(); //List of email recipients
emails.Add(ConfigurationManager.AppSettings["recipientEmail"]); //Add email recipient from App.config
bool showLists = bool.Parse(ConfigurationManager.AppSettings["showLists"]);
bool sendEmail = bool.Parse(ConfigurationManager.AppSettings["sendEmail"]);
bool sendSMS = bool.Parse(ConfigurationManager.AppSettings["sendSMS"]);
int minTempFeels;
int maxTempFeels;
int maxWind;
double averagePOP;
string precip;
string summary;
string precipSuffix = "";
//Store weather data
List<int> fcTempFeels = new List<int>(); //Hourly "feels like" temperature (celsius)
List<int> fcWind = new List<int>(); //Hourly wind speed (km/h)
List<double> fcPrecip = new List<double>(); //Hourly precipitation (mm or cm)
List<int> fcPOP = new List<int>(); ; //Hourly probability of precipitation (%)
List<string> fcSummary = new List<string>(); //Hourly weather description

//Configure Browser
var deviceDriver = ChromeDriverService.CreateDefaultService();
deviceDriver.HideCommandPromptWindow = true;
ChromeOptions options = new ChromeOptions();
options.AddArguments("--disable-infobars");
options.AddArgument("headless");
Global.driver = new ChromeDriver(deviceDriver, options);
Global.driver.Manage().Window.Maximize();
Global.driver.Manage().Window.Size = new System.Drawing.Size(1680, 1280);
//driver.Manage().Window.Minimize();
Global.driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(1);
ChromeDriverService service = ChromeDriverService.CreateDefaultService();
service.HideCommandPromptWindow = true;

//Show the date we are pulling weather for
Console.WriteLine(dateMatcher);
//Browse
Global.driver.Navigate().GoToUrl(weatherURL);
//Find the list of hourly forecast elements on the site
IWebElement element = Global.driver.FindElement(By.XPath("//h2[text()='" + dateMatcher + "']//parent::div//parent::div//parent::div//following-sibling::div"));
if (element != null)
{
    ReadOnlyCollection<IWebElement> elements = element.FindElements(By.XPath("*"));
    foreach(IWebElement el in elements)
    {
        //Get the hour
        IWebElement hour = el.FindElement(By.Id("date-or-time"));
        //Get "feels like" temperature
        IWebElement te = el.FindElement(By.Id("row-feels-like"));
        fcTempFeels.Add(Int32.Parse(te.Text.Replace("Feels ", "")));
        //Get wind speed
        try
        {
            te = el.FindElement(By.Id("wind-gust-value"));
            te = te.FindElement(By.CssSelector("div.sc-fPXMVe.dCuSyu"));
            fcWind.Add(Int32.Parse(te.Text.Split(' ')[0]));
        }
        catch (Exception) //No wind speed available
        {
        }
        //Get POP
        IWebElement popValue = null;
        try
        {
            popValue = el.FindElement(By.Id("pop-value"));
            te = popValue.FindElement(By.CssSelector("div.sc-fPXMVe.dCuSyu"));
            fcPOP.Add(Int32.Parse(te.Text.Replace("%", "")));
        } catch (Exception) { //No POP available
        }
        //Get Precip
        try
        {
            IWebElement tg = el.FindElement(By.Id("precip-values"));
            string oh = tg.GetAttribute("outerHTML");
            if (oh.IndexOf("mm") > -1 && precipSuffix == "")
            {
                precipSuffix = "mm";
            } else if (oh.IndexOf("cm") > -1 && precipSuffix == "")
            {
                precipSuffix = "cm";
            }
            if (precipSuffix != "")
            {
                oh = oh.Split(precipSuffix)[0];
                oh = oh.Substring(oh.LastIndexOf(">") + 1);
            }
            fcPrecip.Add(Convert.ToDouble(oh));
            Console.WriteLine("Precip: " + fcPrecip[fcPrecip.Count - 1].ToString() + precipSuffix);
        } catch (Exception) { //No precip available
        }
        //Get summary
        te = el.FindElement(By.XPath("//*[@data-testid='expanded-row-weather-text']"));
        fcSummary.Add(te.Text);
    }
}

//Calculate overall values
minTempFeels = fcTempFeels.Min();
maxTempFeels = fcTempFeels.Max();
maxWind = fcWind.Max();
summary = FindMostFrequentValue(fcSummary);

//Generate HTML report
reportHTML += "<p>" + Environment.NewLine;
reportHTML += "High (Feels Like): " + maxTempFeels + "°<br/>" + Environment.NewLine;
reportHTML += "Low (Feels Like): " + minTempFeels + "°<br/>" + Environment.NewLine;
reportHTML += "Max Wind: " + maxWind + "km/h<br/>" + Environment.NewLine;
if (fcPOP.Count > 0)
{
    averagePOP = Math.Round(fcPOP.Average());
    reportHTML += "P.O.P.: " + averagePOP + "%<br/>" + Environment.NewLine;
}
if (fcPrecip.Count > 0)
{
    precip = Math.Round(fcPrecip.Sum(), 1) + precipSuffix;
    reportHTML += "Total Precip: " + Math.Round(fcPrecip.Sum(), 1) + precipSuffix + "<br/>" +  Environment.NewLine;
}
summary = FindMostFrequentValue(fcSummary);
reportHTML += "Summary: " + summary + "<br/>" + Environment.NewLine;
reportHTML += "<a href=\"" + weatherURL + "\">Hourly Forecast</a><br/>" + Environment.NewLine;
reportHTML += "</p>" + Environment.NewLine;

//Generate SMS report
reportSMS += Environment.NewLine + "High (Feels Like): " + maxTempFeels + "°" + Environment.NewLine;
reportSMS += "Low (Feels Like): " + minTempFeels + "°" + Environment.NewLine;
reportSMS += "Max Wind: " + maxWind + "km/h" + Environment.NewLine;
if (fcPOP.Count > 0)
{
    averagePOP = Math.Round(fcPOP.Average());
    reportSMS += "P.O.P.: " + averagePOP + "%" + Environment.NewLine;
}
if (fcPrecip.Count > 0)
{
    precip = Math.Round(fcPrecip.Sum(), 1) + precipSuffix;
    reportSMS += "Total Precip: " + Math.Round(fcPrecip.Sum(), 1) + precipSuffix + "" + Environment.NewLine;
}
reportSMS += "Summary: " + summary + "" + Environment.NewLine;
reportSMS += weatherURL + Environment.NewLine;

//Close browser
Global.driver.Close();
Global.driver.Quit();

//Send emails
if (sendEmail) {
    SmtpClient mySmtpClient = new SmtpClient(ConfigurationManager.AppSettings["smtpHost"], Convert.ToInt32(ConfigurationManager.AppSettings["smtpPort"]));
    mySmtpClient.UseDefaultCredentials = false;
    System.Net.NetworkCredential basicAuthenticationInfo = new
    System.Net.NetworkCredential(ConfigurationManager.AppSettings["senderEmail"], ConfigurationManager.AppSettings["senderEmailCred"]);
    mySmtpClient.Credentials = basicAuthenticationInfo;
    mySmtpClient.EnableSsl = true;
    MailAddress from = new MailAddress(ConfigurationManager.AppSettings["senderEmail"], "ToggenWeather");
    MailAddress replyTo = new MailAddress(ConfigurationManager.AppSettings["senderEmail"]);
    foreach (string email in emails)
    {
        try
        {
            MailAddress to = new MailAddress(email, email);
            MailMessage myMail = new System.Net.Mail.MailMessage(from, to);
            myMail.ReplyToList.Add(replyTo);
            myMail.Subject = ConfigurationManager.AppSettings["emailSubjectPrefix"] + " Weather for " + dateMatcher.Split(' ')[1] + " " + dateMatcher.Split(' ')[2] + " (" + maxTempFeels + "°)";
            myMail.SubjectEncoding = System.Text.Encoding.UTF8;
            myMail.Body = reportHTML;
            myMail.BodyEncoding = System.Text.Encoding.UTF8;
            myMail.IsBodyHtml = true;
            mySmtpClient.Send(myMail);
    }
        catch (Exception)
        {

        }
    }
    Console.WriteLine("Email sent");
}

//Send SMS
if (sendSMS)
{
    AmazonSimpleNotificationServiceClient client = new AmazonSimpleNotificationServiceClient(ConfigurationManager.AppSettings["awsID"], ConfigurationManager.AppSettings["awsSec"], RegionEndpoint.USEast2);
    PublishRequest publishRequest = new PublishRequest(ConfigurationManager.AppSettings["awsTopic"], reportSMS);
    PublishResponse publishResponse = await client.PublishAsync(publishRequest);
    Console.WriteLine(publishResponse.HttpStatusCode);
    Console.WriteLine(publishResponse.ContentLength);
    Console.WriteLine(publishResponse.MessageId);
    Console.WriteLine(publishResponse.ResponseMetadata);
    Thread.Sleep(2000);
    Console.WriteLine("SMS sent");
}

//Show captured data
if (showLists)
{
    Console.WriteLine("--START Temp Feels");
    foreach (int i in fcTempFeels)
    {
        Console.WriteLine("Feels like " + i + "°");
    }
    Console.WriteLine("--END Temp Feels");
    Console.WriteLine("--START Wind");
    foreach (int i in fcWind)
    {
        Console.WriteLine(i + "km/h");
    }
    Console.WriteLine("--END Wind");
    Console.WriteLine("--START Precip");
    foreach (double i in fcPrecip)
    {
        Console.WriteLine(i + "mm");
    }
    Console.WriteLine("--END Precip");
    Console.WriteLine("--START POP");
    foreach (int i in fcPOP)
    {
        Console.WriteLine(i + "%");
    }
    Console.WriteLine("--END POP");
    Console.WriteLine("--START Summary");
    foreach (string s in fcSummary)
    {
        Console.WriteLine(s);
    }
    Console.WriteLine("--END Summary");
}
Console.WriteLine("All done!");
Environment.Exit(0);

static string FindMostFrequentValue(List<string> stringList)
{
    if (stringList == null || stringList.Count == 0)
    {
        return null;
    }
    var groupedValues = stringList.GroupBy(s => s);
    var mostFrequent = groupedValues.OrderByDescending(g => g.Count()).FirstOrDefault();
    return mostFrequent?.Key;
}
