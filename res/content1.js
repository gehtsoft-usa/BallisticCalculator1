// Auxilary function to support "expand tree on right frame navigation" functionality
var selectedItem = null;

function getFileFromURL(url) {
    var f = url.split('/');
    return f[f.length - 1];
}

function createIds() {
    var lis = document.getElementsByTagName("li");
    for (var lisi = 0; lisi < lis.length; lisi++) {
        var li = lis[lisi];
        if (li.firstChild.nodeName == "A") {
            var h = li.firstChild.href;
            if (h) {
                li.id = getFileFromURL(h);
            }
        }
    }
    var as = document.getElementsByTagName("a");
    for (var asi = 0; asi < as.length; asi++) {
        var a = as[asi];
        a.onclick = function(event) {
            event = event || window.event;
            selectLink(event.target);
        }
    }
}

function selectLink(lnk) {
    if (selectedItem) {
        selectedItem.className = '';
    }
    lnk.className = "select";
    selectedItem = lnk;
}

function selectItem(fid, tid, title) {
    var id1 = getFileFromURL(fid);
    var id = null;

    if (document.getElementById(id1))
        id = id1;
    else if (tid && document.getElementById(tid))
        id = tid;
    else
        return ;

    expandToItem('hhc_tree', id);
    window.parent.parent.location.hash = id;
    window.parent.parent.document.title = helptitle + ' : ' + title;
    var childs = document.getElementById(id).childNodes;
    for (var ci = 0; ci < childs.length; ci++) {
        if (childs[ci].nodeName == "A") {
            selectLink(childs[ci]);
        }
    }
}