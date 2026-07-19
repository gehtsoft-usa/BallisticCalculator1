function loadStarted() {
    window.onload = loadCompleted;

    if (highlighterEnabled && !window.location.href.indexOf('mk:') == 0) {
        var head = document.getElementsByTagName('head')[0];

        var script4 = document.createElement('script');
        script4.type = 'text/javascript';
        script4.src = './highlighter/highlight.min.js';
        head.appendChild(script4);


        var script2 = document.createElement('script');
        script2.type = 'text/javascript';
        script2.src = './highlighter/highlight.cshtml.js';
        head.appendChild(script2);

        var script3 = document.createElement('script');
        script3.type = 'text/javascript';
        script3.src = './highlighter/highlight.lua.js';
        head.appendChild(script3);

        var script4 = document.createElement('script');
        script4.type = 'text/javascript';
        script4.src = './highlighter/highlight.luax.js';
        head.appendChild(script4);
    }
}

function loadCompleted() {
    if (highlighterEnabled && !window.location.href.indexOf('mk:') == 0) {
        hljs.registerLanguage('cshtml-razor', window.hljsDefineCshtmlRazor);
        hljs.registerLanguage('lua', window.hljsDefineLua);
        hljs.registerLanguage('luax', window.hljsDefineLuaX);
        x = document.querySelectorAll('pre code');
        for (var i = 0; i < x.length; i++) {
            if (x[i].hasAttribute('class')) {
                hljs.highlightBlock(x[i]);
            }
        }
    }
}


function hidediv(divname) {
    document.getElementById(divname).style.display = 'none';
}

function showdiv(divname) {
    document.getElementById(divname).style.display = 'inline';
}

function syncList() {
    if(window.parent.frames[0]) {
        window.parent.frames[0].selectItem(window.location.href, null, document.title);
    }
}

function syncList1(key) {
    if(window.parent.frames[0]) {
        window.parent.frames[0].selectItem(window.location.href, key + '.html', document.title);
    }
}