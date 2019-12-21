var socket;

function openWebSocket() {
    var loc = window.location, new_uri;
    if (loc.protocol === "https:") {
        new_uri = "wss:";
    } else {
        new_uri = "ws:";
    }
    new_uri += "//" + loc.host + '/ws';

    socket = new WebSocket(new_uri);
  
    socket.onopen = function () {
        console.log('INFO: WebSocket opened successfully - socket uri ' + new_uri);
    };

    socket.onclose = function () {
        console.log('INFO: WebSocket closed - socket uri ' + new_uri);
        openWebSocket();
    };

    socket.onmessage = function (event) {
        console.log('INFO: Message received');
        
        var json = JSON.parse(event.data);

        var image = new Image();
        image.onload = function() {
            var ctx = document.getElementById("tile-canvas").getContext("2d");
            ctx.drawImage(image, json.X, json.Y);
        };
        image.setAttribute('src', 'data:image/png;base64,'+ json.ImageBase64);
    };
}

var init = function() {
    var container = $("#container");
    container.empty();

    var canvas = document.createElement("canvas");
    canvas.setAttribute("id", "tile-canvas");
    canvas.width = 4000;
    canvas.height = 4000;
    container.append(canvas);

    openWebSocket();
};

$(function() {
    init();
    
    $("#btnStart").click(function(event){
        event.preventDefault();
        $.get( "/run");
    });
    $("#btnReset").click(function(event){
        event.preventDefault();
        init();
    });
});
