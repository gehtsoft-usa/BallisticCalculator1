function getHash() {
    return window.parent.location.hash.slice(1);
}

function getQueryParam(name)
{
  var query = window.parent.location.search.substring(1);
  var params = query.split("&");
  for (var i = 0;i < params.length; i++) {
    var pair = params[i].split("=");
    if (pair[0] == name) {
      return pair[1];
    }
  }
  return null;
}

var src = getHash() || getQueryParam('key') || maintopic + '.html';
var iframe = document.getElementById("webcontent");
iframe.src = "web-content-main.html?key=" + src;