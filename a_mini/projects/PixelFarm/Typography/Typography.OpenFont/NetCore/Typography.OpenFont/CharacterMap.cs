﻿//Apache2, 2017, WinterDev
//Apache2, 2014-2016, Samuel Carlsson, WinterDev

using System;
namespace Typography.OpenFont
{
    class CharacterMap
    {
        //https://www.microsoft.com/typography/otspec/cmap.htm

        byte _cmapFormat = 4;
        readonly int _segCount;
        readonly ushort[] _startCode; //Starting character code for each segment
        readonly ushort[] _endCode;//Ending character code for each segment, last = 0xFFFF.      
        readonly ushort[] _idDelta; //Delta for all character codes in segment
        readonly ushort[] _idRangeOffset; //Offset in bytes to glyph indexArray, or 0 (not offset in bytes unit)
        readonly ushort[] _glyphIdArray;

        //
        ushort _fmt6_start;
        ushort _fmt6_end;

        private CharacterMap(int segCount, ushort[] startCode, ushort[] endCode, ushort[] idDelta, ushort[] idRangeOffset, ushort[] glyphIdArray)
        {
            _cmapFormat = 4;
            _segCount = segCount;
            _startCode = startCode;
            _endCode = endCode;
            _idDelta = idDelta;
            _idRangeOffset = idRangeOffset;
            _glyphIdArray = glyphIdArray;
        }
        private CharacterMap(ushort startCode, ushort[] glyphIdArray)
        {
            _cmapFormat = 6;
            _glyphIdArray = glyphIdArray;
            this._fmt6_end = (ushort)(startCode + glyphIdArray.Length);
            this._fmt6_start = startCode;
        }
        public static CharacterMap BuildFromFormat4(int segCount, ushort[] startCode, ushort[] endCode, ushort[] idDelta, ushort[] idRangeOffset, ushort[] glyphIdArray)
        {
            return new CharacterMap(segCount, startCode, endCode, idDelta, idRangeOffset, glyphIdArray);
        }
        public static CharacterMap BuildFromFormat6(ushort startCode, ushort[] glyphIdArray)
        {
            return new CharacterMap(startCode, glyphIdArray);
        }
        public ushort PlatformId { get; set; }
        public ushort EncodingId { get; set; }
        public int CharacterToGlyphIndex(UInt32 character)
        {
            return (int)RawCharacterToGlyphIndex(character);
        }

        public uint RawCharacterToGlyphIndex(UInt32 character)
        {
            // TODO: Fast segment lookup using bit operations?
            switch (this._cmapFormat)
            {
                default: throw new NotSupportedException();
                case 4:
                    {
                        for (int i = 0; i < _segCount; i++)
                        {
                            if (_endCode[i] >= character && _startCode[i] <= character)
                            {
                                if (_idRangeOffset[i] == 0)
                                {
                                    return (character + _idDelta[i]) % 65536; // TODO: bitmask instead?
                                }
                                else
                                {
                                    //If the idRangeOffset value for the segment is not 0,
                                    //the mapping of character codes relies on glyphIdArray. 
                                    //The character code offset from startCode is added to the idRangeOffset value.
                                    //This sum is used as an offset from the current location within idRangeOffset itself to index out the correct glyphIdArray value. 
                                    //This obscure indexing trick works because glyphIdArray immediately follows idRangeOffset in the font file.
                                    //The C expression that yields the glyph index is:

                                    //*(idRangeOffset[i]/2 
                                    //+ (c - startCount[i]) 
                                    //+ &idRangeOffset[i])

                                    var offset = _idRangeOffset[i] / 2 + (character - _startCode[i]);
                                    // I want to thank Microsoft for this clever pointer trick
                                    // TODO: What if the value fetched is inside the _idRangeOffset table?
                                    // TODO: e.g. (offset - _idRangeOffset.Length + i < 0)
                                    return _glyphIdArray[offset - _idRangeOffset.Length + i];
                                }
                            }
                        }
                        return 0;
                    }
                case 6:
                    {
                        //The firstCode and entryCount values specify a subrange (beginning at firstCode, length = entryCount)
                        //within the range of possible character codes.
                        //Codes outside of this subrange are mapped to glyph index 0.
                        //The offset of the code (from the first code) within this subrange is used as index to the glyphIdArray,
                        //which provides the glyph index value.

                        if (character >= _fmt6_start && character <= _fmt6_end)
                        {
                            //in range
                            return _glyphIdArray[character - _fmt6_start];
                        }
                        else
                        {
                            return 0;
                        }
                    }
            }

        }
    }
}
