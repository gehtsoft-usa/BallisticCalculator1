#!/usr/bin/env python3
"""
reticle.py — check and preview BallisticCalculator `.reticle` files.

Why this exists: a `.reticle` file is plain BXml, so a wrong attribute name or a
capitalised `True` produces no error at all — the library just silently drops the
value. And an SVG can be produced, but an agent cannot *look* at it. So this tool
does two things careful writing alone cannot:

  check    read the file the way BallisticXmlDeserializer does and report
           everything the deserializer would silently ignore or misread.
  render   reproduce ReticleDrawController + SvgCanvas exactly — same unit
           conversions, same single-precision arithmetic, same integer flooring,
           same stroke clamping — and print an ASCII raster of the result so the
           geometry can be checked without an image viewer.

The SVG this produces is byte-identical to the library's own output for the
reticles in the repository's `data/reticle/` folder. That fidelity is the point:
a preview that rounds differently from the real renderer would hide exactly the
sub-pixel problems worth catching.

No third-party dependencies. Python 3.8+.

    python3 reticle.py check   my.reticle
    python3 reticle.py render  my.reticle -o my.svg
    python3 reticle.py preview my.reticle --cols 100
"""

from __future__ import annotations

import argparse
import math
import struct
import sys
import xml.etree.ElementTree as ET
from dataclasses import dataclass, field
from typing import Dict, List, Optional, Sequence, Tuple

PI = 3.14159265358979  # the literal Gehtsoft.Measurements itself uses


def f32(x: float) -> float:
    """Round to IEEE single precision.

    CoordinateTranslator and IReticleCanvas are written in `float`, and the
    canvas floors coordinates to integers. A value that lands on 13570.9999 in
    double lands on 13571.0 in float, so doing this arithmetic in double would
    shift output coordinates by one unit. Reproducing the narrowing is what keeps
    the preview honest.
    """
    return struct.unpack("f", struct.pack("f", x))[0]


# --------------------------------------------------------------------------
# Units — the exact operation chains from Gehtsoft.Measurements AngularUnit.
#
# Conversion goes value -> radians -> target, in that operation order, because
# `(v / 10800) * PI` and `v * (PI / 10800)` do not round identically. Measurement
# .Convert short-circuits when the units match, so a value already written in the
# target unit is passed through untouched.
# --------------------------------------------------------------------------

def _to_base(v: float, unit: str) -> float:
    if unit == "rad":
        return v
    if unit in ("°", "deg"):
        return (v / 180.0) * PI
    if unit == "moa":
        return (v / 10800.0) * PI
    if unit == "mil":
        return (v / 3200.0) * PI
    if unit == "mrad":
        return v / 1000.0
    if unit == "ths":
        return (v / 3000.0) * PI
    if unit == "in/100yd":
        return math.atan(v / 3600.0)
    if unit == "cm/100m":
        return math.atan(v / 10000.0)
    if unit in ("%", "percent"):
        return math.atan(v / 100.0)
    if unit == "turn":
        return v * 6.28318530717958
    if unit in ("gon", "ᵍ"):
        return (v * 6.28318530717958) / 400.0
    if unit == "arcsec":
        return (v / 648000.0) * PI
    raise ValueError(f"unknown unit '{unit}'")


UNIT_NAMES = ["rad", "°", "deg", "moa", "mil", "mrad", "ths", "in/100yd",
              "cm/100m", "%", "percent", "turn", "gon", "ᵍ", "arcsec"]

# Longest first so "mrad" beats "rad" and "in/100yd" is not cut short — the same
# longest-suffix-wins rule Measurement.TryParseInternal uses.
_SORTED_UNITS = sorted(UNIT_NAMES, key=len, reverse=True)


def parse_mil(text: str) -> float:
    """Parse '0.25mrad' into the renderer's internal unit, Mil (1/6400 circle)."""
    s = (text or "").strip()
    if not s:
        raise ValueError("empty value")
    for name in _SORTED_UNITS:
        if s.endswith(name):
            try:
                value = float(s[: -len(name)].strip())
            except ValueError:
                continue
            if name == "mil":
                return value                       # Convert() is identity here
            return (_to_base(value, name) / PI) * 3200.0
    raise ValueError(
        f"'{text}' has no recognised angular unit suffix (expected a number immediately "
        f"followed by one of: mrad, moa, mil, deg, rad, ths, cm/100m, in/100yd, turn, gon, arcsec)"
    )


def unit_of(text: str) -> Optional[str]:
    s = (text or "").strip()
    for name in _SORTED_UNITS:
        if s.endswith(name):
            try:
                float(s[: -len(name)].strip())
            except ValueError:
                continue
            return name
    return None


def mil_to_mrad(mil: float) -> float:
    return ((mil / 3200.0) * PI) * 1000.0


# --------------------------------------------------------------------------
# Schema — the exact BXml surface of BallisticCalculator.Reticle.Data.
# "M" angular measurement, "S" string, "B" bool, "E:<enum>" enum.
# --------------------------------------------------------------------------

ENUMS = {
    "line-style": ("Solid", "Dashed", "Dotted"),
    "anchor": ("Left", "Right", "Center"),
}

SCHEMA: Dict[str, Dict[str, Tuple[str, bool]]] = {
    "reticle": {
        "name": ("S", True),
        "size-x": ("M", True), "size-y": ("M", True),
        "zero-x": ("M", False), "zero-y": ("M", False),
    },
    "reticle-line": {
        "start-x": ("M", True), "start-y": ("M", True),
        "end-x": ("M", True), "end-y": ("M", True),
        "line-width": ("M", False),
        "line-color": ("S", False),
        "line-style": ("E:line-style", False),
    },
    "reticle-circle": {
        "center-x": ("M", True), "center-y": ("M", True),
        "radius": ("M", True),
        "fill": ("B", False),
        "line-width": ("M", False),
        "color": ("S", False),
        "line-style": ("E:line-style", False),
    },
    "reticle-rectangle": {
        "position-x": ("M", True), "position-y": ("M", True),
        "size-x": ("M", True), "size-y": ("M", True),
        "fill": ("B", False),
        "line-width": ("M", False),
        "color": ("S", False),
        "line-style": ("E:line-style", False),
    },
    "reticle-text": {
        "position-x": ("M", True), "position-y": ("M", True),
        "text-height": ("M", True),
        "text": ("S", True),
        "anchor": ("E:anchor", False),
        "text-color": ("S", False),
    },
    "reticle-path": {
        "fill": ("B", False),
        "line-width": ("M", False),
        "color": ("S", False),
        "line-style": ("E:line-style", False),
    },
    "reticle-path-move-to": {"position-x": ("M", True), "position-y": ("M", True)},
    "reticle-path-line-to": {"position-x": ("M", True), "position-y": ("M", True)},
    "reticle-path-arc": {
        "position-x": ("M", True), "position-y": ("M", True),
        "radius": ("M", True),
        "clockwise": ("B", True),
        "major-arc": ("B", True),
    },
    "bdc": {
        "position-x": ("M", True), "position-y": ("M", True),
        "text-offset": ("M", True),
        "text-height": ("M", True),
    },
}

TOP_ELEMENTS = ("reticle-line", "reticle-circle", "reticle-rectangle",
                "reticle-text", "reticle-path")
PATH_ELEMENTS = ("reticle-path-move-to", "reticle-path-line-to", "reticle-path-arc")

# The colour attribute is spelled differently on every element type, which is the
# single most common authoring mistake, so name the fix instead of only rejecting.
COLOR_ATTR = {
    "reticle-line": "line-color",
    "reticle-circle": "color",
    "reticle-rectangle": "color",
    "reticle-path": "color",
    "reticle-text": "text-color",
}


@dataclass
class Diag:
    level: str          # "error" | "warn" | "note"
    where: str
    message: str

    def __str__(self) -> str:
        tag = {"error": "ERROR", "warn": "WARN ", "note": "note "}[self.level]
        return f"  {tag} {self.where}: {self.message}"


@dataclass
class Reticle:
    """All measurements are held in Mil, the unit the renderer works in."""
    name: str = ""
    size_x: float = 0.0
    size_y: float = 0.0
    zero_x: Optional[float] = None
    zero_y: Optional[float] = None
    elements: List[dict] = field(default_factory=list)
    bdc: List[dict] = field(default_factory=list)


# --------------------------------------------------------------------------
# Parse + validate
# --------------------------------------------------------------------------

def _check_attrs(el: ET.Element, name: str, where: str, diags: List[Diag]) -> Dict[str, object]:
    spec = SCHEMA[name]
    out: Dict[str, object] = {}

    for attr, raw in el.attrib.items():
        if attr not in spec:
            hint = ""
            if attr in ("color", "line-color", "text-color") and name in COLOR_ATTR:
                hint = f" — on <{name}> the colour attribute is '{COLOR_ATTR[name]}'"
            elif attr == "width":
                hint = " — did you mean 'line-width'?"
            elif attr in ("style", "stroke", "dash"):
                hint = " — did you mean 'line-style'?"
            elif attr in ("x", "y", "cx", "cy"):
                hint = " — coordinates are flattened pairs like 'position-x'/'position-y'"
            diags.append(Diag("error", where,
                              f"unknown attribute '{attr}'; the deserializer ignores it "
                              f"silently{hint}"))
            continue

        kind, _ = spec[attr]
        if kind == "M":
            try:
                out[attr] = parse_mil(raw)
            except ValueError as ex:
                diags.append(Diag("error", where, f"{attr}: {ex}"))
        elif kind == "B":
            if raw not in ("true", "false"):
                diags.append(Diag("error", where,
                                  f"{attr}='{raw}' — the deserializer reads a bool as "
                                  f"(text == \"true\"), so anything else silently means FALSE. "
                                  f"Use lowercase 'true' or 'false'."))
                out[attr] = False
            else:
                out[attr] = raw == "true"
        elif kind.startswith("E:"):
            allowed = ENUMS[kind[2:]]
            if raw not in allowed:
                match = [a for a in allowed if a.lower() == raw.lower()]
                hint = (f" — did you mean '{match[0]}'? Enum.Parse is case-sensitive"
                        if match else "")
                diags.append(Diag("error", where,
                                  f"{attr}='{raw}' is not one of {'|'.join(allowed)}{hint}"))
            else:
                out[attr] = raw
        else:
            out[attr] = raw

    for attr, (_, required) in spec.items():
        if required and attr not in el.attrib:
            diags.append(Diag("error", where, f"missing required attribute '{attr}'"))

    return out


def _parse_element(el: ET.Element, index: int, diags: List[Diag]) -> Optional[dict]:
    name = el.tag
    where = f"<{name}> #{index}"
    if name not in TOP_ELEMENTS:
        diags.append(Diag("error", where,
                          f"not a reticle element; expected one of {', '.join(TOP_ELEMENTS)}"))
        return None

    data = _check_attrs(el, name, where, diags)
    data["_kind"] = name
    data["_where"] = where

    if name == "reticle-path":
        segments: List[dict] = []
        holders = [c for c in el if c.tag == "elements"]
        for child in el:
            if child.tag != "elements":
                diags.append(Diag("error", where,
                                  f"unexpected child <{child.tag}>; path segments belong inside "
                                  f"a single <elements> wrapper"))
        if not holders:
            diags.append(Diag("error", where,
                              "no <elements> child; a path needs its segments wrapped in "
                              "<elements>...</elements>"))
        for holder in holders:
            for j, seg in enumerate(holder):
                sub_where = f"{where} segment #{j}"
                if seg.tag not in PATH_ELEMENTS:
                    diags.append(Diag("error", sub_where,
                                      f"<{seg.tag}> is not a path segment; expected one of "
                                      f"{', '.join(PATH_ELEMENTS)}"))
                    continue
                sdata = _check_attrs(seg, seg.tag, sub_where, diags)
                sdata["_kind"] = seg.tag
                segments.append(sdata)
        data["_segments"] = segments

        if segments and segments[0]["_kind"] != "reticle-path-move-to":
            diags.append(Diag("warn", where,
                              "the first segment is not a move-to, so the path starts from "
                              "wherever the previous drawing left off"))
        if len(segments) < 2:
            diags.append(Diag("warn", where,
                              "a path with fewer than two segments draws nothing useful"))

    return data


def load(path: str) -> Tuple[Optional[Reticle], List[Diag]]:
    diags: List[Diag] = []
    try:
        root = ET.parse(path).getroot()
    except ET.ParseError as ex:
        return None, [Diag("error", path, f"not well-formed XML: {ex}")]
    except OSError as ex:
        return None, [Diag("error", path, str(ex))]

    if root.tag != "reticle":
        return None, [Diag("error", "root",
                           f"root element is <{root.tag}>, expected <reticle>")]

    head = _check_attrs(root, "reticle", "<reticle>", diags)
    r = Reticle(
        name=root.get("name", ""),
        size_x=float(head.get("size-x") or 0.0),   # type: ignore[arg-type]
        size_y=float(head.get("size-y") or 0.0),   # type: ignore[arg-type]
        zero_x=head.get("zero-x"),                 # type: ignore[assignment]
        zero_y=head.get("zero-y"),                 # type: ignore[assignment]
    )

    if r.zero_x is None or r.zero_y is None:
        diags.append(Diag("error", "<reticle>",
                          "zero-x and zero-y must both be present. The attribute is marked "
                          "optional and the XML doc claims the centre is used as a default, but "
                          "ReticleDrawController dereferences Zero.X/Zero.Y in its constructor "
                          "and throws NullReferenceException. For a centred zero write size/2."))

    seen = set()
    for section in root:
        if section.tag == "elements":
            seen.add("elements")
            for i, el in enumerate(section):
                parsed = _parse_element(el, i, diags)
                if parsed:
                    r.elements.append(parsed)
        elif section.tag == "bdc":
            seen.add("bdc")
            for i, el in enumerate(section):
                if el.tag != "bdc":
                    diags.append(Diag("error", f"<bdc> #{i}",
                                      f"<{el.tag}> inside the <bdc> collection; each point is "
                                      f"itself a <bdc> element — the wrapper and the item share "
                                      f"the same name"))
                    continue
                data = _check_attrs(el, "bdc", f"<bdc> #{i}", diags)
                data["_where"] = f"<bdc> #{i}"
                r.bdc.append(data)
        else:
            diags.append(Diag("error", "<reticle>",
                              f"unexpected child <{section.tag}>; expected <elements> and "
                              f"optionally <bdc>"))

    if "elements" not in seen:
        diags.append(Diag("error", "<reticle>", "no <elements> section — nothing would be drawn"))

    diags.extend(_semantic_checks(r))
    return r, diags


def _semantic_checks(r: Reticle) -> List[Diag]:
    """Things that parse cleanly but will not look the way they were meant to."""
    out: List[Diag] = []
    if r.size_x <= 0 or r.size_y <= 0:
        out.append(Diag("error", "<reticle>", "size-x and size-y must be positive"))
        return out

    zx = r.zero_x if r.zero_x is not None else r.size_x / 2
    zy = r.zero_y if r.zero_y is not None else r.size_y / 2
    left, right = -zx, r.size_x - zx
    top, bottom = zy, zy - r.size_y          # +y is up, so `top` is the largest y
    unit = r.size_x / 10000.0                # one viewbox unit, in mil

    def m(v: float) -> str:
        return f"{mil_to_mrad(v):.2f}"

    def check_point(where: str, label: str, x: Optional[float], y: Optional[float]) -> None:
        if x is None or y is None:
            return
        if not (left - 1e-9 <= x <= right + 1e-9) or not (bottom - 1e-9 <= y <= top + 1e-9):
            out.append(Diag("warn", where,
                            f"{label} ({m(x)}, {m(y)}) mrad falls outside the field of view "
                            f"x[{m(left)}..{m(right)}] y[{m(bottom)}..{m(top)}] mrad "
                            f"and will be clipped"))

    for e in r.elements:
        where, kind = e["_where"], e["_kind"]

        lw = e.get("line-width")
        if isinstance(lw, float) and 0 < lw < unit:
            out.append(Diag("note", where,
                            f"line-width {m(lw)} mrad is under one viewbox unit "
                            f"({m(unit)} mrad at the default 10000-wide viewbox), so it renders "
                            f"as the 1-unit hairline"))

        if kind == "reticle-line":
            check_point(where, "start", e.get("start-x"), e.get("start-y"))
            check_point(where, "end", e.get("end-x"), e.get("end-y"))
            if ("start-x" in e and e.get("start-x") == e.get("end-x")
                    and e.get("start-y") == e.get("end-y")):
                out.append(Diag("warn", where, "start and end are the same point — draws nothing"))
        elif kind == "reticle-circle":
            check_point(where, "centre", e.get("center-x"), e.get("center-y"))
            rad = e.get("radius")
            if isinstance(rad, float):
                if rad <= 0:
                    out.append(Diag("error", where, "radius must be positive"))
                elif rad / unit < 0.5:
                    out.append(Diag("note", where,
                                    "radius rounds below half a viewbox unit, so SvgCanvas clamps "
                                    "it to a 1-unit dot — intended for a floating dot, surprising "
                                    "otherwise"))
        elif kind == "reticle-rectangle":
            check_point(where, "top-left", e.get("position-x"), e.get("position-y"))
            sx, sy = e.get("size-x"), e.get("size-y")
            if isinstance(sx, float) and isinstance(sy, float):
                if sx <= 0 or sy <= 0:
                    out.append(Diag("error", where,
                                    "size-x and size-y must be positive; the rectangle grows "
                                    "right and DOWN from position"))
                elif isinstance(e.get("position-x"), float):
                    check_point(where, "bottom-right",
                                e["position-x"] + sx, e["position-y"] - sy)
        elif kind == "reticle-text":
            check_point(where, "position", e.get("position-x"), e.get("position-y"))
            if "text-color" not in e:
                out.append(Diag("warn", where,
                                "no text-color. ReticleDrawController passes ReticleText.Color "
                                "straight to the canvas without the 'black' fallback every other "
                                "element gets, so the SVG ends up with an empty fill attribute. "
                                "Set text-color explicitly."))
            if not str(e.get("text", "")).strip():
                out.append(Diag("warn", where, "text is empty"))
        elif kind == "reticle-path":
            for seg in e.get("_segments", []):
                check_point(where, f"{seg['_kind']} position",
                            seg.get("position-x"), seg.get("position-y"))
            if e.get("fill") and e.get("line-width") is not None:
                out.append(Diag("note", where,
                                "line-width is ignored when fill is true — a filled path is drawn "
                                "with no stroke at all"))

    for b in r.bdc:
        check_point(b["_where"], "position", b.get("position-x"), b.get("position-y"))
        if isinstance(b.get("position-y"), float) and b["position-y"] > 0:
            out.append(Diag("note", b["_where"],
                            "a BDC point above zero labels a hold-under, which only applies to "
                            "distances nearer than the zero (closeBdc = true)"))

    out.extend(_bdc_label_checks(r))

    if not r.bdc:
        out.append(Diag("note", "<reticle>",
                        "no <bdc> points. They are invisible anchors rather than drawn marks — "
                        "the app labels them with distances from a trajectory. Add one at each "
                        "drop hold that should carry an auto-generated distance label."))
    return out


# --------------------------------------------------------------------------
# BDC label collision.
#
# A <bdc> anchor draws nothing at design time, so neither the file nor a preview
# shows where its label will land — but at render time DrawBulletDropCompensator
# turns it into a ReticleText at (position-x + text-offset, position-y), and that
# text occupies real space that nothing reserved for it. Overlapping a row numeral
# or a wind hold is invisible until someone renders with a live trajectory, which
# is exactly the kind of failure worth catching mechanically.
# --------------------------------------------------------------------------

# Verdana digit advance is roughly 0.6 em. Distance labels run 2-4 digits, so the
# worst case is assumed: a "1000" is the label most likely to collide.
GLYPH_ADVANCE = 0.6
BDC_LABEL_DIGITS = 4


def _text_bbox(x: float, y: float, height: float, text: str,
               anchor: str = "Left") -> Tuple[float, float, float, float]:
    """Bounding box of a text element. The position is the BASELINE, and glyphs
    extend upward from it, which is why y1 is y + height rather than centred."""
    width = GLYPH_ADVANCE * height * max(1, len(text))
    if anchor == "Center":
        x0 = x - width / 2
    elif anchor == "Right":
        x0 = x - width
    else:
        x0 = x
    return x0, y, x0 + width, y + height


def _element_bbox(e: dict) -> Optional[Tuple[float, float, float, float]]:
    k = e["_kind"]
    pad = (e.get("line-width") or 0.0) / 2
    try:
        if k == "reticle-line":
            xs = (e["start-x"], e["end-x"])
            ys = (e["start-y"], e["end-y"])
            return min(xs) - pad, min(ys) - pad, max(xs) + pad, max(ys) + pad
        if k == "reticle-circle":
            r = e["radius"] + pad
            return e["center-x"] - r, e["center-y"] - r, e["center-x"] + r, e["center-y"] + r
        if k == "reticle-rectangle":
            x0, y0 = e["position-x"], e["position-y"]
            return (min(x0, x0 + e["size-x"]) - pad, min(y0, y0 - e["size-y"]) - pad,
                    max(x0, x0 + e["size-x"]) + pad, max(y0, y0 - e["size-y"]) + pad)
        if k == "reticle-text":
            return _text_bbox(e["position-x"], e["position-y"], e["text-height"],
                              str(e.get("text", "")), e.get("anchor", "Left"))
        if k == "reticle-path":
            pts = [(s["position-x"], s["position-y"]) for s in e.get("_segments", [])]
            if not pts:
                return None
            xs = [p[0] for p in pts]
            ys = [p[1] for p in pts]
            return min(xs) - pad, min(ys) - pad, max(xs) + pad, max(ys) + pad
    except KeyError:
        return None          # a required attribute was missing; already reported
    return None


def _overlap(a: Tuple[float, float, float, float],
             b: Tuple[float, float, float, float]) -> bool:
    return not (a[2] <= b[0] or b[2] <= a[0] or a[3] <= b[1] or b[3] <= a[1])


def _rect_dist_range(box: Tuple[float, float, float, float],
                     cx: float, cy: float) -> Tuple[float, float]:
    """Nearest and farthest distance from a point to a rectangle."""
    x0, y0, x1, y1 = box
    dx = max(x0 - cx, 0.0, cx - x1)
    dy = max(y0 - cy, 0.0, cy - y1)
    near = math.hypot(dx, dy)
    far = max(math.hypot(x - cx, y - cy)
              for x in (x0, x1) for y in (y0, y1))
    return near, far


def _seg_hits_rect(p: Tuple[float, float], q: Tuple[float, float],
                   box: Tuple[float, float, float, float]) -> bool:
    """Does the segment p-q touch the axis-aligned rectangle?"""
    x0, y0, x1, y1 = box
    if _overlap((min(p[0], q[0]), min(p[1], q[1]), max(p[0], q[0]), max(p[1], q[1])), box):
        # Either endpoint inside settles it.
        for pt in (p, q):
            if x0 <= pt[0] <= x1 and y0 <= pt[1] <= y1:
                return True
        # Otherwise test against the four edges.
        corners = [(x0, y0), (x1, y0), (x1, y1), (x0, y1)]
        for i in range(4):
            if _segs_cross(p, q, corners[i], corners[(i + 1) % 4]):
                return True
    return False


def _segs_cross(a: Tuple[float, float], b: Tuple[float, float],
                c: Tuple[float, float], d: Tuple[float, float]) -> bool:
    def cross(o, p, q):
        return (p[0] - o[0]) * (q[1] - o[1]) - (p[1] - o[1]) * (q[0] - o[0])
    d1, d2 = cross(c, d, a), cross(c, d, b)
    d3, d4 = cross(a, b, c), cross(a, b, d)
    return ((d1 > 0) != (d2 > 0)) and ((d3 > 0) != (d4 > 0))


def _point_in_polygon(pt: Tuple[float, float],
                      poly: List[Tuple[float, float]]) -> bool:
    inside = False
    n = len(poly)
    for i in range(n):
        (xi, yi), (xj, yj) = poly[i], poly[(i + 1) % n]
        if (yi > pt[1]) != (yj > pt[1]):
            if pt[0] < (xj - xi) * (pt[1] - yi) / (yj - yi + 1e-300) + xi:
                inside = not inside
    return inside


def _path_hits(box: Tuple[float, float, float, float], e: dict) -> bool:
    """Test a path against its actual outline rather than its bounding box.

    An arc band — a chevron limb, a segmented-circle quadrant — has a bounding
    box far larger than its ink, so a bbox test invents collisions in the empty
    corner. Arcs are approximated by their chord, widened by the arc's sagitta so
    the approximation stays conservative.
    """
    pts = [(s["position-x"], s["position-y"]) for s in e.get("_segments", [])]
    if len(pts) < 2:
        return False

    # How far an arc can bulge from its chord is the sagitta,
    # r - sqrt(r^2 - (d/2)^2) for a chord of length d — usually a small fraction
    # of the radius, so padding by the radius itself would swamp the test.
    pad = (e.get("line-width") or 0.0) / 2
    segs = e.get("_segments", [])
    for idx, s in enumerate(segs):
        if s["_kind"] != "reticle-path-arc" or "radius" not in s or idx == 0:
            continue
        prev = segs[idx - 1]
        d = math.hypot(s["position-x"] - prev["position-x"],
                       s["position-y"] - prev["position-y"])
        rr = max(s["radius"], d / 2)                   # SVG scales r up if too small
        pad = max(pad, rr - math.sqrt(max(0.0, rr * rr - (d / 2) ** 2)))
    grown = (box[0] - pad, box[1] - pad, box[2] + pad, box[3] + pad)

    closed = bool(e.get("fill"))
    edges = list(zip(pts, pts[1:])) + ([(pts[-1], pts[0])] if closed else [])
    for p, q in edges:
        if _seg_hits_rect(p, q, grown):
            return True
    if closed:
        for corner in ((grown[0], grown[1]), (grown[2], grown[1]),
                       (grown[2], grown[3]), (grown[0], grown[3])):
            if _point_in_polygon(corner, pts):
                return True
    return False


def _hits(box: Tuple[float, float, float, float], e: dict) -> bool:
    """Does `box` actually touch the ink of element `e`?

    A bounding box is wrong for an unfilled circle or rectangle: only the outline
    is drawn, so the whole interior is empty. Without this, a field-of-view ring
    would appear to collide with every label inside it.
    """
    eb = _element_bbox(e)
    if eb is None or not _overlap(box, eb):
        return False

    filled = bool(e.get("fill"))
    pad = (e.get("line-width") or 0.0) / 2

    if e["_kind"] == "reticle-circle" and not filled:
        r = e["radius"]
        near, far = _rect_dist_range(box, e["center-x"], e["center-y"])
        return near <= r + pad and far >= r - pad

    if e["_kind"] == "reticle-rectangle" and not filled:
        x0, y0 = e["position-x"], e["position-y"]
        x1, y1 = x0 + e["size-x"], y0 - e["size-y"]
        inner = (min(x0, x1) + pad, min(y0, y1) + pad,
                 max(x0, x1) - pad, max(y0, y1) - pad)
        wholly_inside = (box[0] >= inner[0] and box[2] <= inner[2]
                         and box[1] >= inner[1] and box[3] <= inner[3])
        return not wholly_inside

    if e["_kind"] == "reticle-path":
        return _path_hits(box, e)

    return True


def _bdc_label_checks(r: Reticle) -> List[Diag]:
    out: List[Diag] = []
    labels: List[Tuple[dict, Tuple[float, float, float, float]]] = []

    for b in r.bdc:
        if not all(k in b for k in ("position-x", "position-y", "text-offset", "text-height")):
            continue
        # DrawBulletDropCompensator: x = position.X + TextOffset,
        # y = position.Y - TextHeight/2, and the text always uses the default
        # Left anchor, so it grows to the RIGHT from there whatever the sign of
        # the offset.
        lx = b["position-x"] + b["text-offset"]
        ly = b["position-y"] - b["text-height"] / 2
        box = _text_bbox(lx, ly, b["text-height"], "0" * BDC_LABEL_DIGITS)
        labels.append((b, box))

        hits = [e for e in r.elements if _hits(box, e)]
        if hits:
            what = ", ".join(sorted({h["_kind"] for h in hits}))
            out.append(Diag("warn", b["_where"],
                            f"its distance label (x={mil_to_mrad(lx):.2f} mrad, "
                            f"~{mil_to_mrad(box[2] - box[0]):.2f} mrad wide) overlaps "
                            f"{len(hits)} element(s): {what}"))

    if any(d.level == "warn" for d in out):
        out.append(Diag("note", "<bdc>",
                        "BDC labels are created at render time from a trajectory, so neither this "
                        "file nor its preview shows them — a clash only appears once someone draws "
                        "with live data. Reserve a clear corridor: the label starts at "
                        "position-x + text-offset and grows to the RIGHT whatever the sign of the "
                        "offset, because the labeller never sets an anchor. Putting the drawn row "
                        "numerals on one side and the BDC labels on the other is the usual fix."))

    for i in range(len(labels)):
        for j in range(i + 1, len(labels)):
            if _overlap(labels[i][1], labels[j][1]):
                out.append(Diag("warn", labels[i][0]["_where"],
                                f"its distance label overlaps the one from "
                                f"{labels[j][0]['_where']}"))
    return out


def collect_units(path: str) -> Dict[str, int]:
    counts: Dict[str, int] = {}
    try:
        root = ET.parse(path).getroot()
    except (ET.ParseError, OSError):
        return counts
    for el in root.iter():
        for value in el.attrib.values():
            u = unit_of(value)
            if u:
                counts[u] = counts.get(u, 0) + 1
    return counts


# --------------------------------------------------------------------------
# Rendering — a float-exact port of CoordinateTranslator + SvgCanvas + SvgPath.
# --------------------------------------------------------------------------

class Translator:
    """Port of CoordinateTranslator.

    Two properties of the original are reproduced on purpose: everything is
    single precision, and TransformL always scales by scaleX even for vertical
    lengths — which is why the canvas aspect must match the reticle aspect or
    radii and widths come out distorted.
    """

    def __init__(self, size_x: float, size_y: float, zero_x: float, zero_y: float,
                 dest_w: float, dest_h: float):
        self.sx = f32(f32(dest_w) / f32(size_x))
        self.sy = f32(f32(dest_h) / f32(size_y))
        self.zx = f32(zero_x)
        self.zy = f32(zero_y)

    def point(self, x: float, y: float) -> Tuple[float, float]:
        return (f32(f32(f32(x) + self.zx) * self.sx),
                f32(f32(self.zy - f32(y)) * self.sy))

    def length(self, v: Optional[float]) -> float:
        return 1.0 if v is None else f32(f32(v) * self.sx)


def _i(v: float) -> int:
    return int(math.floor(v))


def _esc(s: str) -> str:
    return (s.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")
             .replace('"', "&quot;"))


def _dash(style: str, width: float) -> str:
    """Port of SvgCanvas.ApplyLineStyle."""
    if style == "Solid":
        return ""
    w = 1.0 if width < 1 else width
    dash = max(1, _i(f32(w) if style == "Dotted" else f32(w * 4)))
    gap = max(1, _i(f32(w * 2) if style == "Dotted" else f32(w * 3)))
    extra = ' stroke-linecap="round"' if style == "Dotted" else ""
    return f' stroke-dasharray="{dash} {gap}"{extra}'


def render_svg(r: Reticle, width: str = "500px", height: str = "500px",
               viewbox_width: int = 10000) -> str:
    zx = r.zero_x if r.zero_x is not None else r.size_x / 2
    zy = r.zero_y if r.zero_y is not None else r.size_y / 2
    # The caller computes the aspect in double, as the C# call site does.
    vbh = _i((r.size_y / r.size_x) * viewbox_width)
    t = Translator(r.size_x, r.size_y, zx, zy, float(viewbox_width), float(vbh))

    body: List[str] = []
    for e in r.elements:
        kind = e["_kind"]
        style = e.get("line-style", "Solid")

        if kind == "reticle-line":
            x1, y1 = t.point(e["start-x"], e["start-y"])
            x2, y2 = t.point(e["end-x"], e["end-y"])
            w = t.length(e.get("line-width"))
            w = 1.0 if w < 1 else w
            body.append(f'<line x1="{_i(x1)}" y1="{_i(y1)}" x2="{_i(x2)}" y2="{_i(y2)}" '
                        f'stroke="{_esc(e.get("line-color") or "black")}" '
                        f'stroke-width="{_i(w)}"{_dash(style, w)} />')

        elif kind == "reticle-circle":
            cx, cy = t.point(e["center-x"], e["center-y"])
            rad = t.length(e["radius"])
            fill = bool(e.get("fill"))
            w = t.length(e.get("line-width"))
            w = 1.0 if (w < 1 and not fill) else w
            color = _esc(e.get("color") or "black")
            body.append(f'<circle cx="{_i(cx)}" cy="{_i(cy)}" '
                        f'r="{_i(1.0 if rad < 0.5 else rad)}" stroke="{color}" '
                        f'stroke-width="{_i(w)}" fill="{color if fill else "none"}"'
                        f'{"" if fill else _dash(style, w)} />')

        elif kind == "reticle-rectangle":
            x0, y0 = t.point(e["position-x"], e["position-y"])
            # The controller passes (x0, y0, x0 + sizeX, y0 + sizeY): +y is down on
            # the canvas, so a rectangle grows right and DOWN from its top-left.
            x1 = f32(x0 + t.length(e["size-x"]))
            y1 = f32(y0 + t.length(e["size-y"]))
            fill = bool(e.get("fill"))
            sw = t.length(e.get("line-width"))
            sw = 1.0 if (sw < 1 and not fill) else sw
            color = _esc(e.get("color") or "black")
            body.append(f'<rect x="{_i(x0)}" y="{_i(y0)}" width="{_i(f32(x1 - x0))}" '
                        f'height="{_i(f32(y1 - y0))}" stroke="{color}" '
                        f'stroke-width="{_i(sw)}" fill="{color if fill else "none"}"'
                        f'{"" if fill else _dash(style, sw)} />')

        elif kind == "reticle-text":
            x0, y0 = t.point(e["position-x"], e["position-y"])
            anchor = {"Left": "start", "Center": "middle",
                      "Right": "end"}[e.get("anchor", "Left")]
            body.append(f'<text x="{_i(x0)}" y="{_i(y0)}" font-family="Verdana" '
                        f'font-size="{_i(t.length(e["text-height"]))}" '
                        f'fill="{_esc(e.get("text-color") or "")}" text-anchor="{anchor}">'
                        f'{_esc(str(e.get("text", "")))}</text>')

        elif kind == "reticle-path":
            # SvgPath separates move-to/line-to/close with a space but writes an arc
            # with no separator at all. Reproduced so the `d` attribute matches.
            d = ""
            for seg in e.get("_segments", []):
                x, y = t.point(seg["position-x"], seg["position-y"])
                if seg["_kind"] == "reticle-path-move-to":
                    d += (" " if d else "") + f"M{_i(x)},{_i(y)}"
                elif seg["_kind"] == "reticle-path-line-to":
                    d += (" " if d else "") + f"L{_i(x)},{_i(y)}"
                else:
                    rr = _i(t.length(seg["radius"]))
                    d += (f"A{rr},{rr} 0 {1 if seg.get('major-arc') else 0},"
                          f"{1 if seg.get('clockwise') else 0} {_i(x)},{_i(y)}")
            fill = bool(e.get("fill"))
            if fill:
                d += (" " if d else "") + "z"
            color = _esc(e.get("color") or "black")
            sw = t.length(e.get("line-width"))
            stroke_bits = "" if fill else f' stroke-width="{_i(sw)}"{_dash(style, sw)}'
            body.append(f'<path d="{d}" fill="{color if fill else "none"}" '
                        f'stroke="{"none" if fill else color}"{stroke_bits} />')

    title = f"<title>{_esc(r.name)}</title>" if r.name else ""
    return (f'<svg xmlns="http://www.w3.org/2000/svg" version="1.1" width="{width}" '
            f'height="{height}" viewBox="0 0 {viewbox_width} {vbh}">{title}'
            + "".join(body) + "</svg>")


# --------------------------------------------------------------------------
# ASCII preview — so the geometry can be inspected without an image viewer.
# --------------------------------------------------------------------------

# Stroke weight is half of what makes a reticle work — the duplex principle is a
# statement about weight, not position — so the raster encodes it in the glyph
# rather than drawing every line the same. Heavier glyphs win an overlap so a fine
# line crossing a post does not erase it; T and + always win because a label or an
# anchor that silently vanished would be the one thing worth seeing.
WEIGHT_GLYPHS = ((0.3, "@"), (0.1, "#"))   # mrad thresholds, descending
FINE_GLYPH = "."
PRIORITY = {" ": 0, ".": 1, "o": 2, ":": 2, "*": 3, "#": 4, "O": 5, "%": 5, "@": 6,
            "T": 9, "+": 9}


def _weight_glyph(width_mil: Optional[float]) -> str:
    if width_mil is None:
        return FINE_GLYPH
    mrad = mil_to_mrad(width_mil)
    for threshold, glyph in WEIGHT_GLYPHS:
        if mrad >= threshold:
            return glyph
    return FINE_GLYPH


def ascii_preview(r: Reticle, cols: int = 78) -> str:
    zx = r.zero_x if r.zero_x is not None else r.size_x / 2
    zy = r.zero_y if r.zero_y is not None else r.size_y / 2
    left, top = -zx, zy
    span_x, span_y = r.size_x, r.size_y
    rows = max(8, int(round(cols * (span_y / span_x) / 2.0)))  # cells are ~2:1 tall
    grid = [[" "] * cols for _ in range(rows)]

    def plot(x: float, y: float, ch: str) -> None:
        cx = int((x - left) / span_x * (cols - 1) + 0.5)
        cy = int((top - y) / span_y * (rows - 1) + 0.5)
        if 0 <= cx < cols and 0 <= cy < rows:
            if PRIORITY.get(ch, 3) >= PRIORITY.get(grid[cy][cx], 0):
                grid[cy][cx] = ch

    def seg(x0: float, y0: float, x1: float, y1: float, ch: str) -> None:
        steps = max(2, int(max(abs(x1 - x0) / span_x * cols,
                              abs(y1 - y0) / span_y * rows) * 3) + 1)
        for i in range(steps + 1):
            k = i / steps
            plot(x0 + (x1 - x0) * k, y0 + (y1 - y0) * k, ch)

    for e in r.elements:
        kind = e["_kind"]
        filled = bool(e.get("fill"))
        if kind == "reticle-line":
            seg(e["start-x"], e["start-y"], e["end-x"], e["end-y"],
                _weight_glyph(e.get("line-width")))
        elif kind == "reticle-circle":
            cx, cy, rad = e["center-x"], e["center-y"], e["radius"]
            glyph = "O" if filled else "o"
            n = max(24, int(rad / span_x * cols * 8))
            for i in range(n + 1):
                a = 2 * math.pi * i / n
                plot(cx + rad * math.cos(a), cy + rad * math.sin(a), glyph)
            if filled:
                plot(cx, cy, glyph)
        elif kind == "reticle-rectangle":
            x0, y0 = e["position-x"], e["position-y"]
            x1, y1 = x0 + e["size-x"], y0 - e["size-y"]
            glyph = "%" if filled else ":"
            seg(x0, y0, x1, y0, glyph); seg(x1, y0, x1, y1, glyph)
            seg(x1, y1, x0, y1, glyph); seg(x0, y1, x0, y0, glyph)
        elif kind == "reticle-text":
            plot(e["position-x"], e["position-y"], "T")
        elif kind == "reticle-path":
            segs = e.get("_segments", [])
            prev: Optional[Tuple[float, float]] = None
            for s in segs:
                pt = (s["position-x"], s["position-y"])
                if s["_kind"] == "reticle-path-move-to":
                    plot(pt[0], pt[1], "*")
                elif prev is not None:
                    # arcs are drawn as their chord in the ASCII view
                    seg(prev[0], prev[1], pt[0], pt[1], "*")
                prev = pt
            if filled and len(segs) > 2:
                seg(segs[-1]["position-x"], segs[-1]["position-y"],
                    segs[0]["position-x"], segs[0]["position-y"], "*")

    for b in r.bdc:
        plot(b["position-x"], b["position-y"], "+")

    def m(v: float) -> str:
        return f"{mil_to_mrad(v):+.1f}"

    out = [
        f"  {r.name or '(unnamed)'} — FOV {mil_to_mrad(span_x):.1f} x "
        f"{mil_to_mrad(span_y):.1f} mrad, zero {mil_to_mrad(zx):.1f} from left / "
        f"{mil_to_mrad(zy):.1f} from top",
        f"  x {m(left)} .. {m(left + span_x)} mrad    y {m(top)} .. {m(top - span_y)} mrad",
        f"  lines by weight: . under 0.1 mrad   # 0.1-0.3   @ over 0.3       "
        f"o/O circle   :/% rect   * path   T text   + bdc anchor",
        "       +" + "-" * cols + "+",
    ]
    for i, row in enumerate(grid):
        y = top - span_y * i / (rows - 1)
        marker = f"{mil_to_mrad(y):+6.1f}" if i % 3 == 0 else "      "
        out.append(f"{marker} |" + "".join(row) + "|")
    out.append("       +" + "-" * cols + "+")
    return "\n".join(out)


# --------------------------------------------------------------------------
# CLI
# --------------------------------------------------------------------------

def _report(path: str, diags: Sequence[Diag]) -> int:
    errors = [d for d in diags if d.level == "error"]
    warns = [d for d in diags if d.level == "warn"]
    notes = [d for d in diags if d.level == "note"]
    print(f"{path}: {len(errors)} error(s), {len(warns)} warning(s), {len(notes)} note(s)")
    for d in list(errors) + list(warns) + list(notes):
        print(d)
    if collect_units(path).get("mil"):
        print("  note  units: this file uses 'mil', which in Gehtsoft.Measurements is the "
              "military mil (1/6400 circle = 3.375 MOA = 0.98175 mrad), NOT the milliradian. "
              "If the design is meant to be a mil/mrad reticle, write 'mrad'.")
    return 1 if errors else 0


def main(argv: Optional[Sequence[str]] = None) -> int:
    p = argparse.ArgumentParser(description=__doc__,
                                formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = p.add_subparsers(dest="cmd", required=True)

    c = sub.add_parser("check", help="validate one or more .reticle files")
    c.add_argument("files", nargs="+")

    rd = sub.add_parser("render", help="write an SVG and print an ASCII preview")
    rd.add_argument("file")
    rd.add_argument("-o", "--output", help="SVG output path (default: <file>.svg)")
    rd.add_argument("--width", default="500px")
    rd.add_argument("--height", default="500px")
    rd.add_argument("--viewbox", type=int, default=10000)
    rd.add_argument("--cols", type=int, default=78)
    rd.add_argument("--no-preview", action="store_true")

    pv = sub.add_parser("preview", help="print only the ASCII preview")
    pv.add_argument("file")
    pv.add_argument("--cols", type=int, default=78)

    a = p.parse_args(argv)

    if a.cmd == "check":
        rc = 0
        for f in a.files:
            _, diags = load(f)
            rc |= _report(f, diags)
        return rc

    r, diags = load(a.file)
    rc = _report(a.file, diags)
    if r is None or rc:
        print("\nFix the errors above before rendering — the library would not read this file "
              "the way it is written.")
        return rc

    if a.cmd == "preview":
        print()
        print(ascii_preview(r, a.cols))
        return 0

    out = a.output or (a.file.rsplit(".", 1)[0] + ".svg")
    svg = render_svg(r, a.width, a.height, a.viewbox)
    with open(out, "w", encoding="utf-8") as fh:
        fh.write(svg)
    print(f"\nwrote {out} ({len(svg)} bytes)")
    if not a.no_preview:
        print()
        print(ascii_preview(r, a.cols))
    return 0


if __name__ == "__main__":
    sys.exit(main())
