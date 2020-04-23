using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using Tweetinvi;
using Tweetinvi.Parameters;
using Windows.ApplicationModel;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
//using Microsoft.UI.Xaml.Media;
//using System.Drawing;
using Windows.UI;
using Windows.UI.Xaml.Media;
using System.Linq;
using System.Threading.Tasks;

namespace HateSpeechDetector.Views
{

    public sealed partial class UserSearchPage : Page, INotifyPropertyChanged
    {
        Windows.UI.Xaml.Media.AcrylicBrush myBrush = new Windows.UI.Xaml.Media.AcrylicBrush();
        public int usertweet = 300;
        public int TweetRate = 0;
        int n = 1; // counter for index of db while reading
        System.Threading.Timer _timer1;
        System.Threading.Timer _timer2;// a timer declaration for use in refreshing displayed tweets
        public ObservableCollection<Item> Items { get; set; } // observable collection type used to collect items for syncronous work.
        private BackgroundWorker streamer = new BackgroundWorker(); // background worker creates a seperate thread
        public static int count = 1; // counter used to count tweets recieved so far
        public bool streaming = false; // check for start stream button. initially false. becomes true when stream starts and becomes false again when stream stops
        public string text = ""; //to store keywords given
        public string dbname = ""; // simply to store keyword but used for naming db
        public int Possible_Hate = 0;
        public UserSearchPage()
        {
            myBrush.BackgroundSource = AcrylicBackgroundSource.HostBackdrop;
            myBrush.FallbackColor = Color.FromArgb(255, 202, 24, 37);
            myBrush.TintOpacity = 0.5;
            myBrush.TintTransitionDuration = System.TimeSpan.FromSeconds(2);
            myBrush.Opacity = 0.5;

            Items = new ObservableCollection<Item>(); // creates observable collection for ITEM class
            DataContext = this; // not sure myself but needed for binding of data to listview from observable collections
            InitializeComponent(); // initializes page

            streamer.WorkerSupportsCancellation = true; // parameter to tell that other thread can be canclled
            streamer.DoWork += new DoWorkEventHandler(worker_DoWork); // do work function declaration of background thread
            streamer.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted); // work completed function of background thread
        }

        private async void Refresh(ObservableCollection<Item> items, string db) // takes observable collections and database name , function used to refresh UI elements in List view
        {

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {

                // Your UI update code goes here!
                string dbpath = Path.Combine(ApplicationData.Current.LocalFolder.Path, db); // path to the db folder + file
                SqliteConnection connection = new SqliteConnection($"Filename={dbpath}");
                connection.Open();
                SqliteCommand command = new SqliteCommand("SELECT * FROM Tweets LIMIT 10 OFFSET " + n + "", connection); // this query takes 1 tweet by index number n

                using (var reader = command.ExecuteReader())
                {
                    var Tweettext = reader.GetOrdinal("text");
                    var UserName = reader.GetOrdinal("username");
                    var picture_url = reader.GetOrdinal("picture_url");
                    var timestamp = reader.GetOrdinal("timestamp");
                    var Pred = reader.GetOrdinal("pred");
                    var ClassVar = reader.GetOrdinal("Class");// takes entries in text column of table
                    while (reader.Read())
                    {

                        //Detected_Quanity.Text = Pred.ToString();
                        if (reader.GetInt32(Pred) > 50)
                        {
                            Possible_Hate++;
                            Detected_Quanity.Text = "Possible Detected = " + Possible_Hate.ToString();
                            Detected_Quanity.Foreground = new SolidColorBrush(Colors.OrangeRed);

                            items.Add(new Item()
                            {
                                text = reader.GetString(Tweettext),
                                username = reader.GetString(UserName),
                                picture_url = reader.GetString(picture_url),
                                timestamp = reader.GetString(timestamp),
                                pred = reader.GetInt32(Pred),
                                ClassVar = reader.GetString(ClassVar),
                                HTCOLOR = myBrush

                            }); // adds each taken tweet to observable collections
                        }
                        else
                        {
                            items.Add(new Item()
                            {
                                text = reader.GetString(Tweettext),
                                username = reader.GetString(UserName),
                                picture_url = reader.GetString(picture_url),
                                timestamp = reader.GetString(timestamp),
                                pred = reader.GetInt32(Pred),
                                ClassVar = reader.GetString(ClassVar),
                                HTCOLOR = new AcrylicBrush()
                            }); // adds each taken tweet to observable collections
                        }


                        n++; // increments index so that next time next entry is taken
                    }
                }
                connection.Close();
            });          //}

        }

        private void worker_RunWorkerCompleted(object _sender, RunWorkerCompletedEventArgs e) // called once work is done by background do work fuunction
        {

            if (e.Cancelled) // checks if thread is stopped manually
            {
                count = 1;
                n = 1;
                streaming = false;
                progress.IsActive = false;
                Detect.Content = "Get Timeline"; // change button text back to default

            }

        }

        public event PropertyChangedEventHandler PropertyChanged;

        public static void AddData(string inputText, string dbname, string pic_url, string username, string timestamp) // used to add data in database , takes input tweet and name of db
        {
            string dbpath = Path.Combine(ApplicationData.Current.LocalFolder.Path, dbname); // gets complete path to db
            using (SqliteConnection db =
              new SqliteConnection($"Filename={dbpath}"))
            {
                db.Open();

                SqliteCommand insertCommand = new SqliteCommand();
                insertCommand.Connection = db;

                // Uses parameterized query method to prevent SQL injection attacks
                insertCommand.CommandText = "INSERT INTO Tweets VALUES (null,@text,@pred,@username,@picture_url,@timestamp,@Class);"; // query to add into table Tweets
                //insertCommand.Parameters.AddWithValue("@ID", "None");
                insertCommand.Parameters.AddWithValue("@text", inputText);
                insertCommand.Parameters.AddWithValue("@pred", 1);
                insertCommand.Parameters.AddWithValue("@username", username);
                insertCommand.Parameters.AddWithValue("@picture_url", pic_url);
                insertCommand.Parameters.AddWithValue("@timestamp", timestamp);
                insertCommand.Parameters.AddWithValue("@Class", " ");
                insertCommand.ExecuteNonQuery();

                db.Close();
            }

        }

        private async void worker_DoWork(object _sender, DoWorkEventArgs e) // do work function for background thread
        {

            List<object> genericlist = e.Argument as List<object>; // takes arguments passed from main thread and saves them in list


            //

            // split text based on comma
            int keyword_index = 0;
            string keywords = genericlist[0].ToString();
            string[] Sep_Keywords = keywords.Split(',');
            int lenght = Sep_Keywords.Length;
            //

            // twiter dev account credentials
            Auth.SetUserCredentials("xSZ8K1M3GC4AI3ZRkTx65o3SR", "hTTwzVSy2C2STTCNuMmyLvIyvuTM0QG8DFLU8AYQdpuBwq9tj1", "1277890010-OvHKa6LU2QMj2BBSa8mcJOQfJ4Atw21BAIWMHPX", "JnKq9eZefyqfL9M7SlE1PbbSlh8Duw5XjvZ7Z5ABPzI1J");

            //var stream = Tweetinvi.Stream.CreateFilteredStream();// creates stream with follows filters

            ////only english tweets
            //stream.AddTweetLanguageFilter(Tweetinvi.Models.LanguageFilter.English);


            //while (lenght > 0) // adds each split keyword as keyword to track
            //{
            //    stream.AddTrack(Sep_Keywords[keyword_index]);
            //    lenght--;
            //    keyword_index++;
            //}

            //stream.MatchingTweetReceived += (sender, theTweet) => // when tweets match filters its taken
            //{

            //    if (!theTweet.Tweet.IsRetweet) // if tweet is not a retweet does following
            //    {
            //        AddData(theTweet.Tweet.FullText, genericlist[1].ToString()); // calls add function and passes tweet text

            //        TweetRate++;

            //        if (TweetRate > 4)
            //        {
            //            TweetRate = 0;
            //            Thread.Sleep(1000); // sleeps this thread for 1 sec to syncronise with UI main thread
            //        }
            //        //

            //        count++; // increments counted tweets
            //        if (count == 100) // if count reaches 100 stops streaming
            //        {
            //            //m_dbConnection.Close();
            //            stream.StopStream();
            //            e.Cancel = true;

            //        }
            //    }

            if (streamer.CancellationPending == true) // if main thread asks this to stop , this if is run
            {
                //m_dbConnection.Close();
                e.Cancel = true;
            }
            //};
            //stream.StartStreamMatchingAllConditions(); // makes stream follow all conditions/ filters


            // Get more control over the request with a UserTimelineParameters
            var userTimelineParameters = new UserTimelineParameters();
            var tweets = Timeline.GetUserTimeline(genericlist[0].ToString(),usertweet);
            

            foreach (var tweet in tweets)
            {

                AddData(tweet.Text, genericlist[1].ToString(), (tweet.CreatedBy.ProfileImageUrl).ToString(), (tweet.CreatedBy).ToString(), (tweet.CreatedAt).ToString()); // calls add function and passes tweet text                   
                count++;
            }
            await Button_ClickAsync();
            Thread.Sleep(10000);
            e.Cancel = true;
  



        }


        private void Set<T>(ref T storage, T value, [CallerMemberName]string propertyName = null)
        {
            if (Equals(storage, value))
            {
                return;
            }

            storage = value;
            OnPropertyChanged(propertyName);
        }

        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));


        public async void Button_Click(object sender, RoutedEventArgs e) // called when button is clicked
        {
            if (streaming == false) // if bool check for streaming is false means not streaming runs these functions
            {
                if(tweetrate.Text.Length!=0)
                {
                    usertweet = Convert.ToInt32(tweetrate.Text);
                }
                Detected_Quanity.Text = "Initializing, Please Wait";
                progress.IsActive = true; // starts progresss ring
                streaming = true; // bool check to true indicating streaming has started
                text = UserHandle.Text; // takes keyword text and saves it into text variable
                dbname = text; // takes keyword and adds .db at end to pass to createdb function below as a name
                dbname += ".db";
                await InitializeDatabase(dbname); // takes db name with extention
                Detect.Content = "Stop Search"; // chnages button text to stop stream

                List<object> arguments = new List<object>(); // makes list of arguments to be passed to background worker thread
                arguments.Add(text); // argument 1 
                arguments.Add(dbname); // arument 2

                streamer.RunWorkerAsync(arguments); // starts background worker

                

                if (streamer.IsBusy) // if background worker is busy then calls refresh function after 2 sec
                {
                    Thread.Sleep(6000); // sleeps thread for 4 sec
                    //_timer2 = new System.Threading.Timer(new System.Threading.TimerCallback((obj) => Button_ClickAsync()), null, 0, 6000);
                    _timer1 = new System.Threading.Timer(new System.Threading.TimerCallback((obj) => Refresh(Items, dbname)), null, 0, 10);
                }
                else
                {
                    n = 1;
                }

            }
            else if (streaming == true) // if already running then runs these functions
            {
                _timer1.Dispose();
                n = 1;
                count = 1;
                progress.IsActive = false; // stops progress ring
                Detect.Content = "Get Timeline"; // changes button text back to start stream
                streamer.CancelAsync(); // cancles thread if manually stopped
                streaming = false; // changes bool check indicating streaming stopped
            }

        }
        private async         Task
Button_ClickAsync()
        {



            if (ApiInformation.IsApiContractPresent("Windows.ApplicationModel.FullTrustAppContract", 1, 0))
            {
                ApplicationData.Current.LocalSettings.Values["parameters"] = Path.Combine(ApplicationData.Current.LocalFolder.Path, dbname);
                await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
            }


        }
        public async static Task InitializeDatabase(string text) // to make database each time a streaming starts , takes keywords as parameter
        {

            await ApplicationData.Current.LocalFolder.CreateFileAsync(text, CreationCollisionOption.ReplaceExisting); // makes file but will replace if already exist to avoid taking older data
            string dbpath = Path.Combine(ApplicationData.Current.LocalFolder.Path, text);
            SqliteConnection db = new SqliteConnection($"Filename={dbpath}");

            db.Open();
            // query to create table named Tweets and two Attributes ID and text
            string tableCommand = "CREATE TABLE IF NOT " +
                     "EXISTS Tweets (_id INTEGER PRIMARY KEY , " +
                     "text NVARCHAR(2048) NULL, " +
                     "pred INTEGER NULL," +
                     "username NVARCHAR(100) NULL," +
                     "picture_url NVARCHAR(150) NULL," +
                     "timestamp NVARCHAR(100) NULL," +
                     "Class NVARCHAR(15) NULL)";

            SqliteCommand createTable = new SqliteCommand(tableCommand, db);

            createTable.ExecuteReader();
            db.Close();

        }
        private void ItemsList_ItemClick(object sender, ItemClickEventArgs e)
        {
            Details.Visibility = Visibility.Visible;
            Item S_item = (Item)e.ClickedItem;

            Profile_Frame.ProfilePicture = new BitmapImage(new Uri(S_item.picture_url, UriKind.Absolute));
            Profile_Name.Text = "User Name: " + S_item.username;
            Profile_Tweet.Text = S_item.text;
            Prediction_Percentage.Value = Convert.ToDouble(S_item.pred);
            //Pred_Class.Text = S_item.ClassVar;
            Pred_Class_text.Text = "Probability of Hate Speech" + " " + S_item.pred.ToString() + " % ";


        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

            Items.Clear();
            Detected_Quanity.Text = " ";
            Details.Visibility = Visibility.Collapsed;
        }

        private void AdvanceLocation_Toggled(object sender, RoutedEventArgs e)
        {
            //
        }

        private void tweetlimit_Toggled(object sender, RoutedEventArgs e)
        {
            if (tweetrate.IsEnabled == true)
            {
                tweetrate.IsEnabled = false;
              
            }
            else {tweetrate.IsEnabled = true; }
        }

        private void tweetrate_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
        {
            args.Cancel = args.NewText.Any(c => !char.IsDigit(c)); // prevents non-numeric input
        }
    }
}
