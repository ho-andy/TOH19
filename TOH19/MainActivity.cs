using System;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Android.Hardware.Camera2;
using Android.Provider;
using Java.IO;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Linq;
using Java.Util;
using Java.Text;
using Xamarin.Forms;

namespace TOH19
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true/*, ScreenOrientation = ScreenOrientation.Landscape*/)]
    public class MainActivity : AppCompatActivity
    {
        const int REQUEST_TAKE_PHOTO = 1;
        const string subscriptionKey = "";
        const string uriBase =
            "https://eastus2.api.cognitive.microsoft.com/vision/v2.0/read/core/asyncBatchAnalyze";z
        static TextView calOutput;
        
        

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);


            Android.Support.V7.Widget.Toolbar toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.my_toolbar);
            SetSupportActionBar(toolbar);
            toolbar.SetTitleTextColor(Android.Graphics.Color.Rgb(255, 255, 255)); 


            calOutput = FindViewById<TextView>(Resource.Id.consoleView);

            calOutput.Text = "Click the camera button below to get started!";
            




            if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.Camera) == (int)Permission.Granted)
            {
                //Toast.MakeText(Application.Context, "took a pic", ToastLength.Short).Show();
                FloatingActionButton fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
                fab.Click += FabOnClick;
                
                
            }
            else
            {
                ActivityCompat.RequestPermissions(this, new String[] { Manifest.Permission.Camera }, 0); //0 = REQUEST CAMERA
                ActivityCompat.RequestPermissions(this, new String[] { Manifest.Permission.ReadCalendar }, 0);
                ActivityCompat.RequestPermissions(this, new String[] { Manifest.Permission.WriteCalendar }, 0);
            }

        }

        public void lookCameraIntent()
        {
            Android.Net.Uri photoURI;
            Intent takePictureIntent = new Intent(MediaStore.ActionImageCapture);
            if(takePictureIntent.ResolveActivity(PackageManager) != null)
            {
                Java.IO.File photoFile = null;
                photoFile = createImageFile();

                photoURI =  FileProvider.GetUriForFile(this, "com.companyname.TOH19.fileprovider", photoFile);
                takePictureIntent.PutExtra(MediaStore.ExtraOutput, photoURI);

                StartActivityForResult(takePictureIntent, REQUEST_TAKE_PHOTO); //1 = REQUEST_TAKE_PHOTO

            }
            
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            //base.OnActivityResult(requestCode, resultCode, data);
            if (requestCode == REQUEST_TAKE_PHOTO)
            {
                //Toast.MakeText(Application.Context, "took a pic", ToastLength.Short).Show();
                addToGallery(); //after photo is created, function call to call API
            }
            
        }

        string photoPath; //absolute path of photo to pass binary to cloud

        public Java.IO.File createImageFile()
        {
            String timeStamp = new SimpleDateFormat("yyyyMMdd_HHmmss").Format(new Date());
            Java.IO.File imageDir = GetExternalFilesDir(Android.OS.Environment.DirectoryPictures);

            Java.IO.File image = Java.IO.File.CreateTempFile("TOH19_IMAGE_" + timeStamp, ".png", imageDir);

            photoPath = image.AbsolutePath;
            return image;
        }

        public void addToGallery()
        {
            //Intent mediaScanIntent = new Intent(Intent.ActionMediaScannerScanFile);
            //File file = new File(photoPath);
            //Android.Net.Uri photoUri = Android.Net.Uri.FromFile(file);
            //mediaScanIntent.SetData(photoUri);
            //this.SendBroadcast(mediaScanIntent);

            //Toast.MakeText(Application.Context, "adding a pic", ToastLength.Short).Show();

            //if (System.IO.File.Exists(photoPath))
            //{
            //    ReadHandwrittenText(photoPath.ToString()).Wait();
            //} else
            //{
            //    Toast.MakeText(Application.Context, photoPath.ToString(), ToastLength.Short).Show();
            //}

            System.Threading.Thread.Sleep(3000); //to deal with race condition of photo & call

            System.Diagnostics.Debug.WriteLine(photoPath.ToString());
            ReadHandwrittenText(photoPath.ToString()); 
        }

        /*static */async Task ReadHandwrittenText(string imageFilePath) //call to azure
        {
            try
            {
                HttpClient client = new HttpClient();

                // Request headers.
                client.DefaultRequestHeaders.Add(
                    "Ocp-Apim-Subscription-Key", subscriptionKey);

                // Assemble the URI for the REST API method.
                string uri = uriBase;

                HttpResponseMessage response;

                // Two REST API methods are required to extract handwritten text.
                // One method to submit the image for processing, the other method
                // to retrieve the text found in the image.

                // operationLocation stores the URI of the second REST API method,
                // returned by the first REST API method.
                string operationLocation = "";

                // Reads the contents of the specified local image
                // into a byte array.
                byte[] byteData = GetImageAsByteArray(imageFilePath);

                // Adds the byte array as an octet stream to the request body.
                using (ByteArrayContent content = new ByteArrayContent(byteData))
                {
                    // This example uses the "application/octet-stream" content type.
                    // The other content types you can use are "application/json"
                    // and "multipart/form-data".
                    content.Headers.ContentType =
                        new MediaTypeHeaderValue("application/octet-stream");

                    // The first REST API method, Batch Read, starts
                    // the async process to analyze the written text in the image.
                    response = await client.PostAsync(uri, content); //json call
                }

                // The response header for the Batch Read method contains the URI
                // of the second method, Read Operation Result, which
                // returns the results of the process in the response body.
                // The Batch Read operation does not return anything in the response body.
                if (response.IsSuccessStatusCode)
                    operationLocation =
                        response.Headers.GetValues("Operation-Location").FirstOrDefault();
                else
                {
                    // Display the JSON error data.
                    string errorString = await response.Content.ReadAsStringAsync();
                    System.Console.WriteLine("\n\nResponse:\n{0}\n",
                        JToken.Parse(errorString).ToString());
                    //return;
                }

                // If the first REST API method completes successfully, the second 
                // REST API method retrieves the text written in the image.
                //
                // Note: The response may not be immediately available. Handwriting
                // recognition is an asynchronous operation that can take a variable
                // amount of time depending on the length of the handwritten text.
                // You may need to wait or retry this operation.
                //
                // This example checks once per second for ten seconds.
                string contentString;
                int i = 0;
                do
                {
                    System.Threading.Thread.Sleep(1000);
                    response = await client.GetAsync(operationLocation);
                    contentString = await response.Content.ReadAsStringAsync();
                    ++i;
                }
                while (i < 10 && contentString.IndexOf("\"status\":\"Succeeded\"") == -1);

                if (i == 10 && contentString.IndexOf("\"status\":\"Succeeded\"") == -1)
                {
                    System.Console.WriteLine("\nTimeout error.\n");
                    //return;
                }

                // Display the JSON response.
                JObject json = JObject.Parse(contentString);
                // Console.WriteLine("\nResponse:\n\n{0}\n",JToken.Parse(contentString).ToString());
                //string month = json.SelectToken("recognitionResults[0].lines[0].text").ToString();
                //var days = new List<string> { "sunday", "monday", "tuesday", "wednesday", "thursday", "friday", "saturday" };
                string[] date = json.SelectToken("recognitionResults[0].lines[0].text").ToString().Split(" ");
                var days = new List<string> { "sunday", "monday", "tuesday", "wednesday", "thursday", "friday", "saturday" };
                var months = new List<string> {"january", "february", "march", "april", "may", "june", "july", "august", "september", "october", "november", "december" };
                int month = months.IndexOf(date[0].ToLower());
                int year = int.Parse(date[1]);
                System.Diagnostics.Debug.WriteLine(month + " " + year);
                int length = json.SelectToken("recognitionResults[0].lines").Count();
                var calendar = new List<Tuple<string, string>> { };
                var appt = new List<Tuple<string, int, int>> { };
                var box = new List<Tuple<string, int, int>> { };
                int curr = 1;
                int j = 1;
                //Console.WriteLine(json.ToString());
                while (j < length)
                {
                    string t = json.SelectToken("recognitionResults[0].lines[" + (j.ToString()) + "].text").ToString();
                    string[] b = json.SelectToken("recognitionResults[0].lines[" + (j.ToString()) + "].boundingBox").ToString().Trim().Split("\n");
                    //Console.WriteLine(json.ToString());
                    //string[] b = string.Join("", c).Split(",");
                    string a = b[1].Trim();
                    string c = b[2].Trim();
                    int x = int.Parse(a.Remove(a.Length - 1));
                    int y = int.Parse(c.Remove(c.Length - 1));
                    if (t == "Co") { t = "8"; }
                    if (curr.ToString() == t)
                    {
                        box.Add(Tuple.Create(t, x, y));
                        //Console.WriteLine(box[curr - 1]);
                        curr++;
                    }
                    else
                    {
                        if (curr != 1 && !int.TryParse(t, out int z) && !days.Contains(t.ToLower()))
                        {
                            appt.Add(Tuple.Create(t, x, y));
                        }
                    }
                    //Console.WriteLine(b[0]);
                    System.Console.WriteLine(t);
                    System.Diagnostics.Debug.WriteLine("t: " + t);
                    j++;
                }
                //Console.WriteLine(box[29]);
                foreach (var tuple in appt)
                {
                    System.Console.WriteLine(tuple);
                    int ind = 0;

                    for (var k = 0; k < box.Count; k++)
                    {
                        if (box[k].Item2 < tuple.Item2 && box[k].Item3 < tuple.Item3)
                        {
                            ind = int.Parse(box[k].Item1);
                        }
                        if (box[k].Item2 > (tuple.Item2 + 10) && (box[k].Item3 > tuple.Item3 || tuple.Item3 < box[k].Item3 + 9))
                        {
                            //Console.WriteLine(ind + " " + box[k].Item2 + " " + tuple.Item2);   
                            calendar.Add(Tuple.Create(box[k - 1 /* - 7*/].Item1, tuple.Item1));
                            break;
                        }
                        if (k == box.Count - 1)
                        {
                            calendar.Add(Tuple.Create(box[box.Count - 1].Item1, tuple.Item1));
                        }

                    }
                }
                calOutput.Text = "";
                foreach (var test in calendar)
                {
                    System.Console.WriteLine(test);
                    System.Diagnostics.Debug.WriteLine(" tuple: " + test);
                    calOutput.Text += "Check your calendar for: " + test + " ";

                    AddAppointment(test.Item2, int.Parse(test.Item1), year, month); //desc, day, year, month

                }

                //Console.WriteLine(month);
            }
            catch (Exception e)
            {
                System.Console.WriteLine("\n" + e.Message);
            }
        }

        public void AddAppointment(string desc, int day, int year, int month)
        {
            Java.Util.TimeZone timeZone = Java.Util.TimeZone.Default;

            ContentValues eventValues = new ContentValues();

            eventValues.Put(CalendarContract.Events.InterfaceConsts.CalendarId, "1");
            eventValues.Put(CalendarContract.Events.InterfaceConsts.Title, "Your AutoCal event");
            eventValues.Put(CalendarContract.Events.InterfaceConsts.Description, desc);
            eventValues.Put(CalendarContract.Events.InterfaceConsts.Dtstart, GetDateTimeMS(year, month, day, 00, 0));
            eventValues.Put(CalendarContract.Events.InterfaceConsts.Dtend, GetDateTimeMS(year, month, day, 23, 59));
            //eventValues.Put(CalendarContract.ExtraEventBeginTime, "00:00");
            //eventValues.Put(CalendarContract.ExtraEventEndTime, "23:59");
            eventValues.Put(CalendarContract.Events.InterfaceConsts.EventTimezone, timeZone.ID);
            eventValues.Put(CalendarContract.Events.InterfaceConsts.EventEndTimezone, timeZone.ID);

            //ContentResolver contResv = getApplicationContext().getContentResolver();
            //Context context = (Context) this.getActivity().getApplicationContext().getContentResolver();
            //ContentResolver result = (ContentResolver)context.getContentResolver();
            Android.Net.Uri uri = ContentResolver.Insert(CalendarContract.Events.ContentUri,eventValues);


            //long startTime = ;
            //Intent intent = new Intent(Intent.ActionInsert);
            //
            //intent.PutExtra(CalendarContract.Events.InterfaceConsts.Title, "New calendarEvent by Team B");
            //intent.PutExtra(CalendarContract.Events.InterfaceConsts.Description, desc);
            //intent.PutExtra(CalendarContract.Events.InterfaceConsts.Dtstart, GetDateTimeMS(year, month, day, 00, 0));
            //intent.PutExtra(CalendarContract.Events.InterfaceConsts.Dtend, GetDateTimeMS(year, month, day, 23, 59));
            //intent.PutExtra(CalendarContract.ExtraEventBeginTime, "00:00");
            //intent.PutExtra(CalendarContract.ExtraEventEndTime, "23:59");
            //intent.PutExtra(CalendarContract.Events.InterfaceConsts.EventTimezone, timeZone.ID);
            //intent.PutExtra(CalendarContract.Events.InterfaceConsts.EventEndTimezone, timeZone.ID);
            //intent.SetData(CalendarContract.Events.ContentUri);
            //((Activity)Forms.Context).StartActivity(intent);
        }

        public long GetDateTimeMS(int yr, int month, int day, int hr, int min) //gets unix time to use in Calendar creator
        {
            Calendar c = Calendar.GetInstance(Java.Util.TimeZone.Default);

            System.Diagnostics.Debug.WriteLine(day);
            System.Diagnostics.Debug.WriteLine(hr);
            System.Diagnostics.Debug.WriteLine(min);
            System.Diagnostics.Debug.WriteLine(month);
            System.Diagnostics.Debug.WriteLine(yr);
            c.Set(Java.Util.CalendarField.DayOfMonth, day);
            c.Set(Java.Util.CalendarField.HourOfDay, hr);
            c.Set(Java.Util.CalendarField.Minute, min);
            c.Set(Java.Util.CalendarField.Month, month);
            c.Set(Java.Util.CalendarField.Year, yr);

            return c.TimeInMillis;
        }



        /// <summary>
        /// Returns the contents of the specified file as a byte array.
        /// </summary>
        /// <param name="imageFilePath">The image file to read.</param>
        /// <returns>The byte array of the image data.</returns>
        static byte[] GetImageAsByteArray(string imageFilePath)
        {
            // Open a read-only file stream for the specified file.
            using (FileStream fileStream =
                new FileStream(imageFilePath, FileMode.Open, FileAccess.Read))
            {
                // Read the file's contents into a byte array.
                BinaryReader binaryReader = new BinaryReader(fileStream);
                return binaryReader.ReadBytes((int)fileStream.Length);
            }
        }



        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int id = item.ItemId;
            if (id == Resource.Id.action_settings)
            {
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        private void FabOnClick(object sender, EventArgs eventArgs)
        {
            Android.Views.View view = (Android.Views.View) sender;
            //Snackbar.Make(view, "Replace with your own action", Snackbar.LengthLong)
            //    .SetAction("Action", (Android.Views.View.IOnClickListener)null).Show();
            lookCameraIntent(); //opens camera to take a photo when button is clicked
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
	}
}

