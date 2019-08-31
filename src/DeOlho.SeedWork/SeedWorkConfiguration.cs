using System;
using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace DeOlho.SeedWork
{
    public class SeedWorkConfiguration 
    {

        public SeedWorkConfiguration(
            string eventBusSectionName = "EventBus",
            string deOlhoContextConnectionString = "DeOlho")
        {

            EventBusSectionName = eventBusSectionName;
            DeOlhoContextConnectionString = deOlhoContextConnectionString;
        }

        public string EventBusSectionName { get; private set; }
        public string DeOlhoContextConnectionString { get; private set; }

    }
}