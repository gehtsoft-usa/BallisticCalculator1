// ===================================================================
// Author: Matt Kruse <matt@mattkruse.com>
// WWW: http://www.mattkruse.com/
//
// NOTICE: You may use this code for any purpose, commercial or
// private, without any further permission from the author. You may
// remove this notice from your final code if you wish, however it is
// appreciated by the author if at least my web site address is kept.
//
// You may *NOT* re-distribute this code in any way except through its
// use. That means, you can include it in your product, or your web
// site, or any other form where the code is actually being used. You
// may not put the plain javascript up on your site for download or
// include it in your javascript libraries for download.
// If you wish to share this code with others, please just point them
// to the URL instead.
// Please DO NOT link directly to my .js files from your site. Copy
// the files to your server and use them there. Thank you.
// ===================================================================

// HISTORY
// ------------------------------------------------------------------
// December 9, 2003: Added script to the Javascript Toolbox
// December 10, 2003: Added the preProcessTrees variable to allow user
//      to turn off automatic conversion of UL's onLoad
// March 1, 2004: Changed it so if a <li> has a class already attached
//      to it, that class won't be erased when initialized. This allows
//      you to set the state of the tree when painting the page simply
//      by setting some <li>'s class name as being "liOpen" (see example)
/*
This code is inspired by and extended from Stuart Langridge's aqlist code:
        http://www.kryogenix.org/code/browser/aqlists/
        Stuart Langridge, November 2002
        sil@kryogenix.org
        Inspired by Aaron's labels.js (http://youngpup.net/demos/labels/)
        and Dave Lindquist's menuDropDown.js (http://www.gazingus.org/dhtml/?id=109)
*/
function addEvent(o,e,f){if(o.addEventListener){o.addEventListener(e,f,true);return true;}else if(o.attachEvent){return o.attachEvent("on"+e,f);}else{return false;}}
function setDefault(name,val){if(typeof(window[name])=="undefined" || window[name]==null){window[name]=val;}}
function expandTree(treeId){var ul = document.getElementById(treeId);if(ul == null){return false;}expandCollapseList(ul,nodeOpenClass);}
function collapseTree(treeId){var ul = document.getElementById(treeId);if(ul == null){return false;}expandCollapseList(ul,nodeClosedClass);}
function expandToItem(treeId,itemId){var ul = document.getElementById(treeId);if(ul == null){return false;}var ret = expandCollapseList(ul,nodeOpenClass,itemId);if(ret){var o = document.getElementById(itemId);if(o.scrollIntoView){o.scrollIntoView(false);}}}
function expandCollapseList(ul,cName,itemId){if(!ul.childNodes || ul.childNodes.length==0){return false;}for(var itemi=0;itemi<ul.childNodes.length;itemi++){var item = ul.childNodes[itemi];if(itemId!=null && item.id==itemId){return true;}if(item.nodeName == "LI"){var subLists = false;for(var sitemi=0;sitemi<item.childNodes.length;sitemi++){var sitem = item.childNodes[sitemi];if(sitem.nodeName=="UL"){subLists = true;var ret = expandCollapseList(sitem,cName,itemId);if(itemId!=null && ret){item.className=cName;return true;}}}if(subLists && itemId==null){item.className = cName;}}}}
function convertTrees(){setDefault("treeClass","mktree");setDefault("nodeClosedClass","liClosed");setDefault("nodeOpenClass","liOpen");setDefault("nodeBulletClass","liBullet");setDefault("nodeLinkClass","bullet");setDefault("preProcessTrees",true);if(preProcessTrees){if(!document.createElement){return;}uls = document.getElementsByTagName("ul");for(var uli=0;uli<uls.length;uli++){var ul=uls[uli];if(ul.nodeName=="UL" && ul.className==treeClass){processList(ul);}}}}
function processList(ul){if(!ul.childNodes || ul.childNodes.length==0){return;}for(var itemi=0;itemi<ul.childNodes.length;itemi++){var item = ul.childNodes[itemi];if(item.nodeName == "LI"){var subLists = false;for(var sitemi=0;sitemi<item.childNodes.length;sitemi++){var sitem = item.childNodes[sitemi];if(sitem.nodeName=="UL"){subLists = true;processList(sitem);}}var s= document.createElement("SPAN");var t= '\u00A0';s.className = nodeLinkClass;if(subLists){if(item.className==null || item.className==""){item.className = nodeClosedClass;}if(item.firstChild.nodeName=="#text"){t = t+item.firstChild.nodeValue;item.removeChild(item.firstChild);}s.onclick = function(){this.parentNode.className =(this.parentNode.className==nodeOpenClass) ? nodeClosedClass : nodeOpenClass;return false;}}else{item.className = nodeBulletClass;s.onclick = function(){return false;}}s.appendChild(document.createTextNode(t));item.insertBefore(s,item.firstChild);}}}
