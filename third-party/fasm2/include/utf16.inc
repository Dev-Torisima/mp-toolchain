;Public Domain (created by Torisima, 2026)
;macro of replace utf-8 with utf-16 (including null char, 0)
macro utf16 str
{
    local bytes, ch, i

    bytes = str
    i = 0

    while bytes#i <> 0
        ch = bytes#i

        if ch < 0x80
            dw ch
            i = i + 1
        else if (ch and 0xE0) = 0xC0
            dw ((ch and 0x1F) shl 6) or (bytes#(i+1) and 0x3F)
            i = i + 2
        else if (ch and 0xF0) = 0xE0
            dw ((ch and 0x0F) shl 12) or \
               ((bytes#(i+1) and 0x3F) shl 6) or \
               (bytes#(i+2) and 0x3F)
            i = i + 3
        else if (ch and 0xF8) = 0xF0
            local codepoint, high, low
            codepoint = ((ch and 0x07) shl 18) or \
                        ((bytes#(i+1) and 0x3F) shl 12) or \
                        ((bytes#(i+2) and 0x3F) shl 6) or \
                        (bytes#(i+3) and 0x3F)
            codepoint = codepoint - 0x10000
            high = 0xD800 or (codepoint shr 10)
            low  = 0xDC00 or (codepoint and 0x3FF)
            dw high, low
            i = i + 4
        end if
    end while

    dw 0
}
