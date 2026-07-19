/*
Language: Lua
Description: Lua is a powerful, efficient, lightweight, embeddable scripting language.
Author: Andrew Fedorov <dmmdrs@mail.ru>
Category: common, scripting
Website: https://www.lua.org
*/

function hljsDefineLuaX(hljs) {
  const COMMENTS = [
    hljs.COMMENT('--', '$'),
  ];
  return {
    name: 'LuaX',
    keywords: {
      $pattern: hljs.UNDERSCORE_IDENT_RE,
      literal: "true false nil",
      keyword: "and break do else elseif end for goto if in local not or repeat return then until while this super class public internal private class var int real string boolean void const static datetime new function try catch throw",
      built_in:
        // Metatags and globals:
        'stdlib io file list assert int_map string_map queue stack'
    },
    contains: COMMENTS.concat([
      hljs.C_NUMBER_MODE,
      hljs.QUOTE_STRING_MODE,
    ])
  };
}