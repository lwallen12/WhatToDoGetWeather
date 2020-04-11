using Dapper;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WhatToDoGetWeather
{
    public class NewFunction
    {
        string _connStr = "server=test1.ce8cn9mhhgds.us-east-1.rds.amazonaws.com;user=Wallen;database=whattodo;port=3306;password=MyRDSdb1";

        string _updateStatement = @"
                
                    UPDATE Weather SET
                          ConditionDateTime = @ConditionDateTime,
                          LastUpdate = @LastUpdate,
                          Temperature = @Temperature,
                          FeelsLike = @FeelsLike,
                          Pressure = @Pressure,
                          Humidity = @Humidity,
                          MainDescription = @MainDescription,
                          WeatherDescription = @WeatherDescription,
                          CloudCount = @CloudCount,
                          WindSpeed = @WindSpeed,
                          WindDirection = @WindDirection,
                          RainFall = @RainFall,
                          Pod = @Pod
                    WHERE LocationId = @LocationId AND TimeFrameHour = @TimeFrameHour";

        public async Task MainAsync()
        {

            List<int> locations = new List<int>() { 4682991, 4692856, 5117949, 4683416 };

            //First, I will get the data from API
            string stringObject;

            using (var connection = new MySqlConnection(_connStr))
            {
                await connection.OpenAsync();

                foreach (int location in locations)
                {

                    //http://api.openweathermap.org/data/2.5/forecast?id=4692856&APPID=f0cf5d0c897c6f47fde6d097b184acb7

                    string apiPath = "http://api.openweathermap.org/data/2.5/forecast?id=" + location + "&APPID=f0cf5d0c897c6f47fde6d097b184acb7";
                    WebRequest requestObject = WebRequest.Create(apiPath);
                    requestObject.Method = "GET";

                    //If require creds
                    //requestObject.Credentials = new NetworkCredential("username", "password");

                    HttpWebResponse responseObject = null;
                    responseObject = (HttpWebResponse)requestObject.GetResponse();

                    using (Stream stream = responseObject.GetResponseStream())
                    {
                        StreamReader sr = new StreamReader(stream);

                        stringObject = sr.ReadToEnd();

                        //sr.Close();
                    }


                    WeatherRootObject weatherRootObject = JsonConvert.DeserializeObject<WeatherRootObject>(stringObject);

                    //Next I will iterate through the Rootobjet I have created and insert values into the db

                    int timeframe = 6;
                    int i = 0;
                    while (i <= 40)
                    {

                        City city = weatherRootObject.city;
                        List<List> weatherList = weatherRootObject.list;

                        Console.WriteLine(city.name);

                        var conditionDateTime = Convert.ToDateTime(weatherList[i].dt_txt);
                        var lastUpdate = DateTime.Now;
                        var temperature = (((weatherList[i].main.temp - 273.15) * 9 / 5) + 32);
                        var feelsLike = (((weatherList[i].main.feels_like - 273.15) * 9 / 5) + 32);
                        var pressure = weatherRootObject.list[i].main.pressure;
                        var humidity = weatherList[i].main.humidity;
                        string mainDescription = weatherList[i].weather[0].main;
                        string weatherDescription = weatherList[i].weather[0].description;
                        var cloudCount = weatherRootObject.list[i].clouds.all;
                        var windDegrees = weatherRootObject.list[i].wind.deg;
                        var windSpeed = weatherRootObject.list[i].wind.speed;
                        var pod = weatherList[i].sys.pod;
                        var rainfall = 0.0;

                        var dtText = weatherRootObject.list[i].dt_txt;

                        try
                        {
                            rainfall = weatherRootObject.list[i].rain.__invalid_name__3h;

                        }
                        catch //(Exception ex)
                        {
                            Console.WriteLine("No rain predicted");
                        }

                        //You know what I say? Install Dapper and do wanna them updates if ya feel
                        //with an entire weather object

                        WeatherCondition weatherCondition = new WeatherCondition()
                        {
                            ConditionDateTime = conditionDateTime,
                            LastUpdate = lastUpdate,
                            Temperature = temperature,
                            FeelsLike = feelsLike,
                            Pressure = pressure,
                            Humidity = humidity,
                            MainDescription = mainDescription,
                            WeatherDescription = weatherDescription,
                            CloudCount = cloudCount,
                            WindDirection = windDegrees,
                            WindSpeed = windSpeed,
                            Pod = pod,
                            RainFall = rainfall,
                            LocationId = location,
                            TimeFrameHour = timeframe
                        };

                        await connection.ExecuteAsync(_updateStatement, weatherCondition);

                        //where location = location and timeframehour = timeframehour
                        i++;
                        timeframe = timeframe + 3;

                    }

                }

            }
            

        }
    }


    public class WeatherCondition
    {
        public int LocationId { get; set; }
        public int TimeFrameHour { get; set; }
        public DateTime? ConditionDateTime { get; set; }
        public DateTime? LastUpdate { get; set; }
        public double Temperature { get; set; }
        public double FeelsLike { get; set; }
        public double Pressure { get; set; }
        public int Humidity { get; set; }
        public string MainDescription { get; set; }
        public string WeatherDescription { get; set; }
        public int CloudCount { get; set; }
        public double WindSpeed { get; set; }
        public double WindDirection { get; set; }
        public double RainFall { get; set; }
        public string Pod { get; set; }
    }
}
