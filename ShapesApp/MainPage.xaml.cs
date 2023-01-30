using Newtonsoft.Json;
/* COMMENT out the line to use Approov SDK */
using System.Net.Http;
/* UNCOMMENT the lines bellow to use Approov SDK */
//using Approov;
namespace ShapesApp;

public partial class MainPage : ContentPage
{
    /* The endpoint version being used: v1 unprotected and v3 for Approov API protection */
    static string endpointVersion = "v1";
    /* The Shapes URL */
    string shapesURL = "https://shapes.approov.io/" + endpointVersion + "/shapes/";
    /* The Hello URL */
    string helloURL = "https://shapes.approov.io/" + endpointVersion + "/hello/";
    /* The secret key: REPLACE with shapes_api_key_placeholder if using SECRETS-PROTECTION */
    string shapes_api_key = "yXClypapWNHIifHUWmBIyPFAm";
    /* COMMENT this line if using Approov */
    private static HttpClient httpClient;
    /* UNCOMMENT this line if using Approov */
    //private static ApproovHttpClient httpClient;

    public MainPage()
	{
        InitializeComponent(); 
        /* COMMENT out the line to use Approov SDK */
        httpClient = new HttpClient();
        /* UNCOMMENT the lines bellow to use Approov SDK */
        //ApproovService.Initialize("<enter-your-config-string-here>");
        //httpClient = ApproovService.CreateHttpClient();
        // Add substitution header: Uncomment if using SECRETS-PROTECTION
        //ApproovService.AddSubstitutionHeader("Api-Key", null);
        httpClient.DefaultRequestHeaders.Add("Api-Key", shapes_api_key);
        
	}

	private async void OnHelloButtonClicked(object sender, EventArgs e)
	{
		Console.WriteLine("HelloButton clicked!");
        HttpResponseMessage response;
        try {
            response = await httpClient.GetAsync(helloURL).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                var cont = await response.Content.ReadAsStringAsync();
                var values = JsonConvert.DeserializeObject<Dictionary<string, string>>(cont);
                if (values.ContainsKey("text")) {
                    // Set status image
                    Application.Current.MainPage.Dispatcher.Dispatch(() => logoImage.Source = "hello.png");
                    // Set status label
                    Application.Current.MainPage.Dispatcher.Dispatch(() => textMessage.Text = values["text"]);
                }
            } else {
                // Set status image
                Application.Current.MainPage.Dispatcher.Dispatch(() => logoImage.Source = "confused.png");
                // Set status label
                Application.Current.MainPage.Dispatcher.Dispatch(() => textMessage.Text = "Error getting Hello from Shapes server");
            }
        } catch (Exception ex) {
            // Set status image
            Application.Current.MainPage.Dispatcher.Dispatch(() => logoImage.Source = "confused.png");
            // Set status label
            Application.Current.MainPage.Dispatcher.Dispatch(() => textMessage.Text = "Exception getting Hello from Shapes server");
        }
	}

    private async void OnShapeButtonClicked(System.Object sender, System.EventArgs e)
    {
        Console.WriteLine("ShapeButton clicked!");
        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync(shapesURL).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                var cont = await response.Content.ReadAsStringAsync();
                var values = JsonConvert.DeserializeObject<Dictionary<string, string>>(cont);
                if (values.ContainsKey("shape"))
                {
                    string shapeImageName = values["shape"].ToLower() + ".png";
                    // Set status image
                    Application.Current.MainPage.Dispatcher.Dispatch(() => logoImage.Source = shapeImageName);
                    // Set status label
                    Application.Current.MainPage.Dispatcher.Dispatch(() => textMessage.Text = response.StatusCode.ToString());
                }
            }
            else
            {
                // Set status image
                Application.Current.MainPage.Dispatcher.Dispatch(() => logoImage.Source = "confused.png");
                // Set status label
                Application.Current.MainPage.Dispatcher.Dispatch(() => textMessage.Text = "Error getting Shape: " + response.StatusCode.ToString());
            }
        }
        catch (Exception ex)
        {
            // Set status image
            Application.Current.MainPage.Dispatcher.Dispatch(() => logoImage.Source = "confused.png");
            // Set status label
            Application.Current.MainPage.Dispatcher.Dispatch(() => textMessage.Text = "Exception getting Shape: " + ex.Message);
        }
    }

}


