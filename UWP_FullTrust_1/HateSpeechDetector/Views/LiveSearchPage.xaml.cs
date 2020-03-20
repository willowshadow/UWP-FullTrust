﻿using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Tweetinvi;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
namespace HateSpeechDetector.Views
{
    public class Item //class used to get item given by streaming function and save its text
    {
        public string text { get; set; }   // this is get set function for saving or reading value stored in attribute text
    }

    public sealed partial class LiveSearchPage : Page, INotifyPropertyChanged
    {
        int n = 1; // counter for index of db while reading
        System.Threading.Timer _timer; // a timer declaration for use in refreshing displayed tweets
        public ObservableCollection<Item> Items { get; set; } // observable collection type used to collect items for syncronous work.
        private BackgroundWorker streamer = new BackgroundWorker(); // background worker creates a seperate thread
        public static int count = 1; // counter used to count tweets recieved so far
        public bool streaming = false; // check for start stream button. initially false. becomes true when stream starts and becomes false again when stream stops
        public string text = ""; //to store keywords given
        public string dbname = ""; // simply to store keyword but used for naming db 
        public LiveSearchPage()
        {
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
                SqliteCommand command = new SqliteCommand("SELECT * FROM Tweets LIMIT 1 OFFSET " + n + "", connection); // this query takes 1 tweet by index number n

                using (var reader = command.ExecuteReader())
                {
                    var Tweettext = reader.GetOrdinal("text"); // takes entries in text column of table
                    while (reader.Read())
                    {
                        items.Add(new Item() { text = reader.GetString(Tweettext) }); // adds each taken tweet to observable collections
                        n++; // increments index so that next time next entry is taken
                    }
                }
                connection.Close();
            });          //}

        }

        private void worker_RunWorkerCompleted(object _sender, RunWorkerCompletedEventArgs e) // called once work is done by background do work fuunction
        {

            Detect.Content = "Start Stream"; // changes button text back to default
            if (e.Cancelled) // checks if thread is stopped manually
            {
                Detect.Content = "Start Stream"; // change button text back to default
            }

        }

        public event PropertyChangedEventHandler PropertyChanged;

        public static void AddData(string inputText, string dbname) // used to add data in database , takes input tweet and name of db
        {
            string dbpath = Path.Combine(ApplicationData.Current.LocalFolder.Path, dbname); // gets complete path to db
            using (SqliteConnection db =
              new SqliteConnection($"Filename={dbpath}"))
            {
                db.Open();

                SqliteCommand insertCommand = new SqliteCommand();
                insertCommand.Connection = db;

                // Uses parameterized query method to prevent SQL injection attacks
                insertCommand.CommandText = "INSERT INTO Tweets VALUES (@ID,@text);"; // query to add into table Tweets
                insertCommand.Parameters.AddWithValue("@ID", count);
                insertCommand.Parameters.AddWithValue("@text", inputText);

                insertCommand.ExecuteNonQuery();

                db.Close();
            }

        }

        private void worker_DoWork(object _sender, DoWorkEventArgs e) // do work function for background thread
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

            var stream = Tweetinvi.Stream.CreateFilteredStream();// creates stream with follows filters

            //only english tweets
            stream.AddTweetLanguageFilter(Tweetinvi.Models.LanguageFilter.English);


            while (lenght > 0) // adds each split keyword as keyword to track
            {
                stream.AddTrack(Sep_Keywords[keyword_index]);
                lenght--;
                keyword_index++;
            }

            stream.MatchingTweetReceived += (sender, theTweet) => // when tweets match filters its taken
            {

                if (!theTweet.Tweet.IsRetweet) // if tweet is not a retweet does following
                {
                    AddData(theTweet.Tweet.FullText, genericlist[1].ToString()); // calls add function and passes tweet text


                    Thread.Sleep(1000); // sleeps this thread for 1 sec to syncronise with UI main thread
                    //

                    count++; // increments counted tweets
                    if (count == 100) // if count reaches 100 stops streaming
                    {
                        //m_dbConnection.Close();
                        stream.StopStream();
                        e.Cancel = true;
                    }
                }

                if (streamer.CancellationPending == true) // if main thread asks this to stop , this if is run
                {
                    //m_dbConnection.Close();
                    stream.StopStream();
                    e.Cancel = true;
                }
            };
            stream.StartStreamMatchingAllConditions(); // makes stream follow all conditions/ filters



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


        public void Button_Click(object sender, RoutedEventArgs e) // called when button is clicked
        {
            if (streaming == false) // if bool check for streaming is false means not streaming runs these functions
            {
                progress.IsActive = true; // starts progresss ring
                streaming = true; // bool check to true indicating streaming has started
                text = Keyword.Text; // takes keyword text and saves it into text variable
                dbname = text; // takes keyword and adds .db at end to pass to createdb function below as a name
                dbname += ".db";
                InitializeDatabase(dbname); // takes db name with extention
                Detect.Content = "Stop Stream"; // chnages button text to stop stream

                List<object> arguments = new List<object>(); // makes list of arguments to be passed to background worker thread
                arguments.Add(text); // argument 1 
                arguments.Add(dbname); // arument 2

                streamer.RunWorkerAsync(arguments); // starts background worker
                Thread.Sleep(1000); // sleeps thread for 1 sec
                Button_ClickAsync();
                if (streamer.IsBusy) // if background worker is busy then calls refresh function after 1 sec
                {
                    _timer = new System.Threading.Timer(new System.Threading.TimerCallback((obj) => Refresh(Items, dbname)), null, 0, 1090);
                }

            }
            else if (streaming == true) // if already running then runs these functions
            {
                progress.IsActive = false; // stops progress ring
                Detect.Content = "Start Stream"; // changes button text back to start stream
                streamer.CancelAsync(); // cancles thread if manually stopped
                streaming = false; // changes bool check indicating streaming stopped
            }

        }
        private async void Button_ClickAsync()
        {
            


            if (ApiInformation.IsApiContractPresent("Windows.ApplicationModel.FullTrustAppContract", 1, 0))
            {
                await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
            }


        }
        public async static void InitializeDatabase(string text) // to make database each time a streaming starts , takes keywords as parameter
        {

            await ApplicationData.Current.LocalFolder.CreateFileAsync(text, CreationCollisionOption.ReplaceExisting); // makes file but will replace if already exist to avoid taking older data
            string dbpath = Path.Combine(ApplicationData.Current.LocalFolder.Path, text);
            SqliteConnection db = new SqliteConnection($"Filename={dbpath}");
            
                await db.OpenAsync();
                // query to create table named Tweets and two Attributes ID and text
                string tableCommand = "CREATE TABLE IF NOT " +
                    "EXISTS Tweets (ID INTEGER AUTO_INCREMENT PRIMARY KEY , " +
                    "text NVARCHAR(2048) NULL)";

                SqliteCommand createTable = new SqliteCommand(tableCommand, db);

                createTable.ExecuteReader();
                db.Close();
            
        }
    }
}
