using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

using Amazon.Lambda.Core;
using Dapper;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace WhatToDoGetWeather
{
    public class Function
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

        /// <summary>
        /// A simple function that takes a string and does a ToUpper
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public void FunctionHandler(ILambdaContext context)
        {




            List<int> locations = new List<int>() { 4682991, 4692856, 5117949, 4683416 };

            //First, I will get the data from API
            string stringObject;

            using (var connection = new MySqlConnection(_connStr))
            {
                connection.Open();

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
                    while (i < 40)
                    {

                        City city = weatherRootObject.city;
                        List<List> weatherList = weatherRootObject.list;

                        Console.WriteLine(city.name);

                        var conditionDateTime = Convert.ToDateTime(weatherList[i].dt_txt);
                        var lastUpdate = DateTime.Now;
                        lastUpdate = lastUpdate.AddHours(-6);
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

                        connection.Execute(_updateStatement, weatherCondition);

                        //where location = location and timeframehour = timeframehour
                        i++;
                        timeframe = timeframe + 3;

                    }

                }

            }

            //Conroe: 4682991
            //Galveston: 4692856
            //Freeport: 5117949
            //Corpus: 4683416

            //List<string> locations = new List<string>() { "4682991", "4692856", "5117949", "4683416" };

            ////First, I will get the data from API
            //string stringObject;


            ////Connection info
            //string connStr = "server=test1.ce8cn9mhhgds.us-east-1.rds.amazonaws.com;user=Wallen;database=whattodo;port=3306;password=MyRDSdb1";
            //MySqlConnection conn = new MySqlConnection(connStr);


            //conn.Open();

            //foreach (string location in locations)
            //{

            //    //http://api.openweathermap.org/data/2.5/forecast?id=4692856&APPID=f0cf5d0c897c6f47fde6d097b184acb7
            //    string apiPath = "http://api.openweathermap.org/data/2.5/forecast?id=" + location + "&APPID=f0cf5d0c897c6f47fde6d097b184acb7";
            //    WebRequest requestObject = WebRequest.Create(apiPath);
            //    requestObject.Method = "GET";

            //    //If require creds
            //    //requestObject.Credentials = new NetworkCredential("username", "password");

            //    HttpWebResponse responseObject = null;
            //    responseObject = (HttpWebResponse)requestObject.GetResponse();

            //    using (Stream stream = responseObject.GetResponseStream())
            //    {
            //        StreamReader sr = new StreamReader(stream);

            //        stringObject = sr.ReadToEnd();

            //        //sr.Close();
            //    }

            //    var times = Enumerable.Range(0, 40).ToList();
            //    WeatherRootObject weatherRootObject = JsonConvert.DeserializeObject<WeatherRootObject>(stringObject);

            //    //Next I will iterate through the Rootobjet I have created and insert values into the db
            //    int i = 0;

            //    foreach (int time in times)
            //    {

            //        City city = weatherRootObject.city;
            //        List<List> WeatherList = weatherRootObject.list;

            //        Console.WriteLine(city.name);

            //        var temperature = (((WeatherList[time].main.temp - 273.15) * 9 / 5) + 32);
            //        var pressure = weatherRootObject.list[time].main.pressure;
            //        var humidity = WeatherList[time].main.humidity;
            //        string mainDescription = WeatherList[time].weather[0].main;
            //        string weatherDescription = WeatherList[time].weather[0].description;
            //        var cloudCount = weatherRootObject.list[time].clouds.all;
            //        var windDegrees = weatherRootObject.list[time].wind.deg;
            //        var windSpeed = weatherRootObject.list[time].wind.speed;
            //        var rainfall = 0.0;

            //        var dtText = weatherRootObject.list[time].dt_txt;
            //        var dtDate = Convert.ToDateTime(dtText);

            //        // if (weatherRootObject.list[23].rain.__invalid_name__3h.Equals(null))
            //        try
            //        {
            //            Console.WriteLine("Rainfall in inches: " + weatherRootObject.list[time].rain.__invalid_name__3h);
            //            rainfall = weatherRootObject.list[time].rain.__invalid_name__3h;

            //        }

            //        catch //(Exception ex)
            //        {
            //            Console.WriteLine("Rainfall in inches not forecasted");
            //        }



            //        Console.WriteLine("------------------------------------------------");

            //        Console.WriteLine("Date Text = {0}, Date DateTime = {1}", dtText, dtDate);
            //        Console.WriteLine("---temperature: " + temperature);
            //        Console.WriteLine("---pressure: " + pressure);
            //        Console.WriteLine("---humidity: " + humidity);
            //        Console.WriteLine("---description: " + mainDescription);
            //        Console.WriteLine("---cloud count: " + cloudCount);
            //        Console.WriteLine("---wind Degrees: " + windDegrees);
            //        Console.WriteLine("---wind speed: " + windSpeed);
            //        Console.WriteLine("---rainfall: " + rainfall);
            //        Console.WriteLine("---date: " + dtDate);

            //        i++;
            //        Console.WriteLine("i is now: " + i);
            //        Console.WriteLine("------------------------------------------------");

            //        //Update where Row is equal to group = i and weatherobject = 'name?'
            //        //or instead of i, use time


            //        //string strTempSql = "UPDATE WeatherCondition SET CurrentStatus = " + temperature + ", WeatherDescription = " + weatherDescription + ", ConditionDateTime = \"" + dtText + "\"" + " WHERE Name = 'Temperature' AND  TimeGroupId = " + time + " AND LocationId = " + location;
            //        //string strPressSql = "UPDATE WeatherCondition SET CurrentStatus = " + pressure + ", ConditionDateTime = \"" + dtText + "\"" + " WHERE Name = 'Pressure' AND  TimeGroupId = " + time + " AND LocationId = " + location;
            //        //string strHumSql = "UPDATE WeatherCondition SET CurrentStatus = " + humidity + ", ConditionDateTime = \"" + dtText + "\"" + " WHERE Name = 'Humidity' AND  TimeGroupId = " + time + " AND LocationId = " + location;
            //        //string strMainDesc = "UPDATE WeatherCondition SET CurrentStatus = \"" + mainDescription + "\"" + ", ConditionDateTime = \"" + dtText + "\"" + " WHERE Name = 'Weather Description' AND  TimeGroupId = " + time + " AND LocationId = " + location;
            //        //string strCloudCount = "UPDATE WeatherCondition SET CurrentStatus = " + cloudCount + ", ConditionDateTime = \"" + dtText + "\"" + " WHERE Name = 'Clouds' AND  TimeGroupId = " + time + " AND LocationId = " + location;
            //        //string strWindDegrees = "UPDATE WeatherCondition SET CurrentStatus = " + windDegrees + ", ConditionDateTime = \"" + dtText + "\"" + " WHERE Name = 'Wind Direction' AND  TimeGroupId = " + time + " AND LocationId = " + location;
            //        //string strWindDirection = "UPDATE WeatherCondition SET CurrentStatus = " + windSpeed + ", ConditionDateTime = \"" + dtText + "\"" + " WHERE Name = 'Wind Speed' AND TimeGroupId = " + time + " AND LocationId = " + location;
            //        ////string strRainFall = "UPDATE WeatherCondition SET CurrentStatus = " + rainfall + ", ConditionDateTime = \"" + dtText + "\"" + " WHERE Name = 'Rain' AND  TimeGroupId = " + time + " AND LocationId = " + location;

            //        //string strDateTime = "UPDATE WeatherCondition SET CurrentStatus = " + dtText + " WHERE Name =  ";

            //        //string strPressSql = "UPDATE WeatherCondition SET CurrentStatus = " + pressure + ", ConditionDateTime = \"" + dtText + "\"" + " WHERE Name = 'Pressure' AND  TimeGroupId = " + time + " AND LocationId = " + location;
            //        //string strHumSql = "UPDATE WeatherCondition SET CurrentStatus = " + humidity + ", ConditionDateTime = \"" + dtText + "\"" + " WHERE Name = 'Humidity' AND  TimeGroupId = " + time + " AND LocationId = " + location;
            //        //string strMainDesc = "UPDATE WeatherCondition SET CurrentStatus = \"" + mainDescription + "\"" + ", ConditionDateTime = \"" + dtText + "\"" + " WHERE Name = 'Weather Description' AND  TimeGroupId = " + time + " AND LocationId = " + location;
            //        string strCloudCount = "UPDATE WeatherCondition SET CurrentStatus = " + cloudCount + ", ConditionDateTime = \"" + dtText + "\"" + " WHERE Name = 'Clouds' AND  TimeGroupId = " + time + " AND LocationId = " + location;
            //        string strWindDegrees = "UPDATE WeatherCondition SET CurrentStatus = " + windDegrees + ", ConditionDateTime = \"" + dtText + "\"" + " WHERE Name = 'Wind Direction' AND  TimeGroupId = " + time + " AND LocationId = " + location;
            //        string strWindDirection = "UPDATE WeatherCondition SET CurrentStatus = " + windSpeed + ", ConditionDateTime = \"" + dtText + "\"" + " WHERE Name = 'Wind Speed' AND TimeGroupId = " + time + " AND LocationId = " + location;
            //        string strRainFall = "UPDATE WeatherCondition SET CurrentStatus = " + rainfall + ", ConditionDateTime = \"" + dtText + "\"" + " WHERE Name = 'Rain' AND  TimeGroupId = " + time + " AND LocationId = " + location;

            //        string strTempSql = $"UPDATE WeatherCondition SET CurrentStatus = '{temperature}', WeatherDescription = '{weatherDescription}', ConditionDateTime = '{dtText}' WHERE Name = 'Temperature' AND  TimeGroupId = {time} AND LocationId = {location};";
            //        string strPressSql = $"UPDATE WeatherCondition SET CurrentStatus = '{pressure}', ConditionDateTime = '{dtText}' WHERE Name = 'Pressure' AND  TimeGroupId = '{time}' AND LocationId = '{location}'";
            //        string strHumSql = $"UPDATE WeatherCondition SET CurrentStatus = '{humidity}', ConditionDateTime = '{dtText}' WHERE Name = 'Humidity' AND  TimeGroupId = '{time}' AND LocationId = '{location}'";
            //        string strMainDesc = $"UPDATE WeatherCondition SET CurrentStatus = '{mainDescription}', ConditionDateTime = '{dtText}' WHERE Name = 'Humidity' AND  TimeGroupId = '{time}' AND LocationId = '{location}'";

            //        MySqlCommand myTemp = new MySqlCommand(strTempSql, conn);
            //        myTemp.ExecuteNonQuery();


            //        MySqlCommand myPressSql = new MySqlCommand(strPressSql, conn);
            //        myPressSql.ExecuteNonQuery();

            //        MySqlCommand myHumSql = new MySqlCommand(strHumSql, conn);
            //        myHumSql.ExecuteNonQuery();

            //        MySqlCommand myMainDesc = new MySqlCommand(strMainDesc, conn);
            //        myMainDesc.ExecuteNonQuery();

            //        MySqlCommand myCloudCount = new MySqlCommand(strCloudCount, conn);
            //        myCloudCount.ExecuteNonQuery();

            //        MySqlCommand myWindDegrees = new MySqlCommand(strWindDegrees, conn);
            //        myWindDegrees.ExecuteNonQuery();

            //        MySqlCommand myWindDirection = new MySqlCommand(strWindDirection, conn);
            //        myWindDirection.ExecuteNonQuery();

            //        MySqlCommand myRainFall = new MySqlCommand(strRainFall, conn);
            //        myRainFall.ExecuteNonQuery();



            //        Console.WriteLine("Done");
        }

            }
        }
   
