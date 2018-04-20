//------------------
//News Article Provider Settings js file
//Dec 2011 brc
//mods
// 22 mar 2013 remove licensing code
//-------------------

//main jquery handler for page
jQuery(document).ready(function ($) {


    //hook up click event to checkbox(es)
    jQuery(".nap_noDnnPagePath").click(function () {
        var clicked = jQuery(this).children(':checkbox');
        jQuery(".nap_noDnnPagePath input[type='checkbox']").each(function () {
            var other = jQuery(this);
            //if not the same as the clicked one, uncheck
            if (other.attr('id') != clicked.attr('id')) {
                this.checked = false;
            }

        });
    });

});         //end main jquery handler for page
//get parameter from querystring
function getParameterByName(name) {
    name = name.replace(/[\[]/, "\\\[").replace(/[\]]/, "\\\]");
    var regexS = "[\\?&]" + name + "=([^&#]*)";
    var regex = new RegExp(regexS);
    var results = regex.exec(window.location.href);
    if (results == null)
        return "";
    else
        return decodeURIComponent(results[1].replace(/\+/g, " "));
}
