﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using log4net;
using Newtonsoft.Json;
using log4net.Config;
using ParseJson.DoAirPriceFee;
using ParseJson.DoAirPriceFee.DoAirPriceClasses;
using ParseJson.DoBooking;
using ParseJson.DoAirPrice;


namespace ParseJson
{

    [TestClass]
    public class BasicBooking
    {
        public static string ApplicationID = "Vueling.TestCore";
        public const string Env = "PRE";

        [TestMethod]
        public void TestBasicBooking()
        {
            //Variables                
            var Date = DateTime.Now.AddDays(7);
            string doairpriceresponsestring, Doairpricefeeresponsestring, dobookingresponsestring, baseAddressDoAirPrice,
                   baseAddressDoAirPriceAndFee, baseAddressDoBooking;

            //Objetos usados en codigo
            DoAirPriceRequest doairpricerequest;
            DoAirPriceResponse doAirPriceResponse;
            DoAirPriceFeeResponse DoAirPriceAndFeeResponseObjecto;
            DoAirPriceFeeRequest Doairpricefeerequest;
            DoBookingRequest BookingrequestObject;
            DobookingResponse BookingresponseObject;
            DirectoryofURL fileURL;
            Contacts Contact = new Contacts();
            Journey currentJourney = new Journey();
            BuscarVuelo flightsearch = new BuscarVuelo();
            object Empty;
            FillSSR ssrcode = new FillSSR();           
            FileManager Fileobject = new FileManager();

            //Buscar archivos para la prueba
            string configfile = Fileobject.FindFiles("*log4net.xml");
            var whatever = Fileobject.LoadLog4netXmlDocument(configfile, Env, ApplicationID).DocumentElement;
            Console.WriteLine(whatever);
            XmlConfigurator.Configure(whatever);
            var log = LogManager.GetLogger(ApplicationID);            

            //Initcializar los logs
            log.Debug(string.Format("***** SERVICE INITIALIZED: {0} *****", ApplicationID));

            //DoAirPrice
            string filelocationDoAirPrice = Fileobject.FindFiles("*DoAirPrice.json");

            //DoPriceFee
            string filelocationDoAirPriceFee = Fileobject.FindFiles("*DoPriceAndFee.json");

            //DoBooking
            string filelocationDoBooking = Fileobject.FindFiles("*DoBooking.json");

            //Objeto de envio y recepcción
            SendFile envio = new SendFile();
            Parse LeerJson = new Parse();

            //Busqueda de endpoints
            string filelocationURL = Fileobject.FindFiles("*Urls.json");
            Empty = LeerJson.FileRequest(filelocationURL, "file");
            fileURL = (DirectoryofURL)Empty;
            Empty = null;

            //URls
            switch (Env)
            {
                case "PRE":
                    baseAddressDoAirPrice = fileURL.URL[0].DoAirPrice;
                    baseAddressDoAirPriceAndFee = fileURL.URL[0].DoAirPriceFee;
                    baseAddressDoBooking = fileURL.URL[0].DoBooking;
                    break;
                case "INT":
                    baseAddressDoAirPrice = fileURL.URL[1].DoAirPrice;
                    baseAddressDoAirPriceAndFee = fileURL.URL[1].DoAirPriceFee;
                    baseAddressDoBooking = fileURL.URL[1].DoBooking;
                    break;
                case "PRO":
                    baseAddressDoAirPrice = fileURL.URL[3].DoAirPrice;
                    baseAddressDoAirPriceAndFee = fileURL.URL[3].DoAirPriceFee;
                    baseAddressDoBooking = fileURL.URL[3].DoBooking;
                    break;
            }

            //Creación del objeto de request doairprice
            Empty = LeerJson.FileRequest(filelocationDoAirPrice, "DoAirPrice");
            doairpricerequest = (DoAirPriceRequest)Empty;
            doairpricerequest.AirportDateTimeList[0].MarketDateDeparture = Date;
            Empty = null;

            //Recepcción del request de do air price response
            doairpriceresponsestring = envio.SendArchivo(baseAddressDoAirPrice, doairpricerequest);
            doAirPriceResponse = JsonConvert.DeserializeObject<DoAirPriceResponse>(doairpriceresponsestring);

            //Selección del vuelo que usaremos para la pruebas
            currentJourney = flightsearch.FindconnFlight(doAirPriceResponse);
            log.Info("The journey picked is: " + currentJourney.JourneySellKey);
            log.Info("The fare picked is:" + currentJourney.JourneyFare[0].JourneyFareKey);

            //DoPriceFee manipulación del objecto y envio de request
            Empty = LeerJson.FileRequest(filelocationDoAirPriceFee, "DoAirPriceFee");
            Doairpricefeerequest = (DoAirPriceFeeRequest)Empty;
            Empty = null;
            Doairpricefeerequest.SellKeyList[0].FareKey = currentJourney.JourneyFare[0].JourneyFareKey;
            Doairpricefeerequest.SellKeyList[0].JourneyKey = currentJourney.JourneySellKey;
            Doairpricefeerequest.PaxInfoList = doairpricerequest.Paxs;
            Doairpricefeeresponsestring = envio.SendArchivo(baseAddressDoAirPriceAndFee, Doairpricefeerequest);

            //Llenado de objecto de respuesta del DoAirPRice
            DoAirPriceAndFeeResponseObjecto = JsonConvert.DeserializeObject<DoAirPriceFeeResponse>(Doairpricefeeresponsestring);
            Console.WriteLine("Resultado JourneySellKey del price and fee: " + DoAirPriceAndFeeResponseObjecto.Price.JourneysPrice[0].JourneySellKey);

            //Booking creación y manipulación del objeto
            Empty = LeerJson.FileRequest(filelocationDoBooking, "DoBooking");
            BookingrequestObject = (DoBookingRequest)Empty;
            Empty = null;

            //Llenar los campos necesarios para crear el request de booking
            BookingrequestObject.SellKeyList[0].JourneyKey = currentJourney.JourneySellKey;
            BookingrequestObject.SellKeyList[0].FareKey = currentJourney.JourneyFare[0].JourneyFareKey;
            
            
            BookingrequestObject.SellKeyList[0].PaxSSRList = ssrcode.FillingSSr(doairpricerequest).PaxSSRList; 
            BookingrequestObject.JourneyList[0] = currentJourney;
            BookingrequestObject.segmentInfo = currentJourney.Segments;

            //Paxinfolist
            var BookingrequestObjectaux = Contact.FillPaxInfo(doairpricerequest);
            BookingrequestObject.PaxInfoList = BookingrequestObjectaux.PaxInfoList;

            //BookingInfoList
            BookingrequestObject.BookingContact = Contact.FillContact(BookingrequestObject);

            //DoBookin                
            dobookingresponsestring = envio.SendArchivo(baseAddressDoBooking, BookingrequestObject);
            BookingresponseObject = JsonConvert.DeserializeObject<DobookingResponse>(dobookingresponsestring);
            //Console.WriteLine("Response de Booking RecordLocator: " + BookingresponseObject.Success.RecordLocator);
            Console.WriteLine("Response de Booking RecordLocator: " + dobookingresponsestring);

            log.Info("The record Locator is: " + BookingresponseObject.Success.RecordLocator);


            log.Debug(string.Format("***** SERVICE FINALIZED: {0} *****", ApplicationID));
        }


    }
}
