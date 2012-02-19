﻿/**
* Base namespace for the application.
*
* @package     Parking.UI.Scripts
* @author      The JSONs
* @copyright   2012
* @license     Propietary
*/

namespace("Parking.App.Collections");

(function ($, collections, undefined) {
    
    collections.SpaceCollection = Backbone.Collection.extend({

        model : Parking.App.Models.Space,

        getDeleted: function() {
            return this.filter(function(space){ return photo.get('Deleted'); });
        },

        getAll: function() {
            return this.without.apply(this, this.getDeleted());
        }
        
    });

})(jQuery, Parking.App.Collections);