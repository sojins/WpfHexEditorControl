#!/usr/bin/env python3
# -*- coding: utf-8 -*-

from struct import pack

def generate_codepage(f, codepoint, endblock, pack_str):
    for i in range(codepoint, endblock):
        b = []
        b = pack(pack_str, i)

        try:
            n = b.decode('shift_jis_2004')
            c='%02X' % (i)
            s=f'{c}={n}\n'
            #print (s)
            f.write(s)
        except:
            pass

f = open(r'shift_jis_2004_python3.tbl', 'w', encoding='utf8')

# ASCII - dot char, non-printable
dots = [0x0D, 0x0A, 0x00]
for i in dots:
    c='%02X' % (i)
    n='.'
    s=f'{c}={n}\n'
    f.write(s)

# ASCII - single byte
code_point=0x20
endblock=0x7F
generate_codepage(f, code_point, endblock, '>B')

# # Single byte
# code_point=0xA0
# endblock=0xF0
# generate_codepage(f, code_point, endblock, '>B')

shift_jis_codeset = [
    {"code_point": 0x8140, "endblock": 0x81FF},    # Special character
    {"code_point": 0x8340, "endblock": 0x82F2},    # Eng-Hiragana
    {"code_point": 0x8340, "endblock": 0x879F},    # Katakana-Special character
    {"code_point": 0x8890, "endblock": 0xEAA0},    # Hanja
    {"code_point": 0xFA40, "endblock": 0xFC40},    # Hanja - 2
    ]

for codeObj in shift_jis_codeset:
    cp = codeObj['code_point']
    eb = codeObj['endblock']
    generate_codepage(f, cp, eb, '>H')

f.close()