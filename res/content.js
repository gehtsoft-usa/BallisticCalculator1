function getQueryParam(name) {
    var query = window.parent.location.search.substring(1);
    var params = query.split("&");
    for (var i = 0; i < params.length; i++) {
        var pair = params[i].split("=");
        if (pair[0] == name) {
            return pair[1];
        }
    }
    return null;
}

addEvent(window, "load", convertTrees);
var src = getQueryParam('key') || maintopic + '.html';
parent.docframe.location = src;

