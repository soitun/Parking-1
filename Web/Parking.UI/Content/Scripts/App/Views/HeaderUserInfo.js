﻿/**
* Base namespace for the application.
*
* @package     Parking.UI.Scripts
* @author      The JSONs
* @copyright   2012
* @license     Propietary
*/

namespace("Parking.App.Views");

(function ($, views, undefined) {

    views.HeaderUserInfo = Backbone.View.extend({

        template: Parking.Configuration.ClientTemplatesUrl + "Shared/HeaderUserInfo.html",
        
        render: Parking.Common.RenderViewTemplate,

        initialize: function() {
            this.model.on("change", this.render, this);
        },

        events: {
           "click .js-logout": "logout"
        },

        "logout": function(e) {
            Parking.App._user.destroy(function() {  
                Parking.App.router.navigate("login", true);
            }); 
            
            return false;
        }

    });


})(jQuery, Parking.App.Views);