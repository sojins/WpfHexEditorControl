#!/usr/bin/env python3
# -*- coding: utf-8 -*-

from struct import pack

def generate_codepage(f, codepoint, endblock, pack_str):
    for i in range(codepoint, endblock):
        b = []
        b = pack(pack_str, i)

        try:
            n = b.decode('euc-kr')
            c='%02X' % (i)
            s=f'{c}={n}\n'
            #print (s)
            f.write(s)
        except:
            pass

f = open(r'EUCKR_python3.tbl', 'w', encoding='utf8')

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

# Special character
code_point=0xA1A0
endblock=0xACF0
generate_codepage(f, code_point, endblock, '>H')

# Hangul
code_point=0xB0A0
endblock=0xC8FF
generate_codepage(f, code_point, endblock, '>H')

# Hanja
code_point=0xCAA0
endblock=0xFDFF
generate_codepage(f, code_point, endblock, '>H')

f.close()