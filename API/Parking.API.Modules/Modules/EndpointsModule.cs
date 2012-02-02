﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using Nancy;
using Nancy.ViewEngines.Razor;
using Nancy.Serializers.Json;
using Sieena.Parking.API.Models;

namespace Sieena.Parking.API.Modules
{
    public class EndpointsModule : AbstractBaseModule
    {
        public EndpointsModule()
            : base("Endpoints")
        {
        }

        [Api("/GetAll", ApiMethod.GET)]
        public List<Endpoint> GetAll(dynamic parameters)
        {
            return Endpoint.GetAll();
        }
    }
}