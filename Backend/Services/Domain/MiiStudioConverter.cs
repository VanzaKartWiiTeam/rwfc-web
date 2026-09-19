using System.Text;

namespace RetroRewindWebsite.Services.Domain;

/// <summary>
/// Converts a Wii Mii store block into the encoded payload the Mii Studio render endpoint expects.
/// </summary>
/// <remarks>
/// This is the step that used to be delegated to miicontestp.wii.rc24.xyz. Converting the block was
/// the only thing that service did for us, and it began answering "Invalid request." to everything,
/// which left every player without a cached avatar stuck with no image at all. Doing it in process
/// also removes one of the two network round trips per render.
///
/// The field mapping is ported from the VanzaKart launcher
/// (Launcher/Services/MiiFileParserService.BuildStudioData), which renders Miis this way already.
/// </remarks>
public static class MiiStudioConverter
{
    // A Wii store block is 0x4A bytes, but every field read below sits under 0x36, so a
    // shorter block is still usable as long as it reaches that far.
    private const int MinimumBlockLength = 0x36;

    // Studio has no direct equivalent for the Wii's single "facial feature" value, so it is
    // split across two separate slots. Index is the Wii value, entry is the Studio one.
    private static readonly int[] MakeupMap = [0, 1, 6, 9, 0, 0, 0, 0, 0, 10, 0, 0, 0, 0, 0, 0];
    private static readonly int[] WrinklesMap = [0, 0, 0, 0, 5, 2, 3, 7, 8, 0, 9, 11, 0, 0, 0, 0];

    /// <summary>
    /// Converts a base64 Wii Mii block into Studio data, returning false when the input is missing,
    /// not valid base64, or too short to be a Mii block.
    /// </summary>
    public static bool TryConvert(string? miiDataBase64, out string studioData)
    {
        studioData = string.Empty;

        if (string.IsNullOrWhiteSpace(miiDataBase64))
            return false;

        byte[] raw;
        try
        {
            raw = System.Convert.FromBase64String(miiDataBase64);
        }
        catch (FormatException)
        {
            return false;
        }

        if (raw.Length < MinimumBlockLength)
            return false;

        studioData = ToStudioData(raw);
        return true;
    }

    /// <summary>
    /// Converts a raw Wii Mii store block into the hex-encoded Studio payload.
    /// </summary>
    public static string ToStudioData(byte[] raw)
    {
        ArgumentNullException.ThrowIfNull(raw);
        if (raw.Length < MinimumBlockLength)
            throw new ArgumentException($"A Wii Mii block must be at least {MinimumBlockLength} bytes.", nameof(raw));

        // The source is never mutated: callers hand us data straight out of the database.
        var block = new byte[raw.Length];
        Buffer.BlockCopy(raw, 0, block, 0, raw.Length);

        // Studio only knows three mouth colours, the Wii has four. Anything above the third
        // renders as an invalid request, so it is folded down before mapping.
        var mouthValue = ReadUInt16BigEndian(block, 0x2E);
        if (((mouthValue >> 9) & 0x03) > 2)
        {
            mouthValue &= 0xFDFF;
            WriteUInt16BigEndian(block, 0x2E, mouthValue);
        }

        var studio = new byte[46];

        var basic = ReadUInt16BigEndian(block, 0);
        studio[0x16] = (byte)(((basic >> 14) & 1) == 1 ? 1 : 0);
        studio[0x15] = (byte)((basic >> 1) & 0xF);
        studio[0x1E] = block[0x16];
        studio[0x02] = block[0x17];

        var face = ReadUInt16BigEndian(block, 0x20);
        var facialFeature = (int)((face >> 6) & 0x0F);
        studio[0x13] = (byte)(face >> 13);
        studio[0x11] = (byte)((face >> 10) & 0x07);
        studio[0x14] = (byte)WrinklesMap[Math.Clamp(facialFeature, 0, WrinklesMap.Length - 1)];
        studio[0x12] = (byte)MakeupMap[Math.Clamp(facialFeature, 0, MakeupMap.Length - 1)];

        var hair = ReadUInt16BigEndian(block, 0x22);
        var hairColor = (int)((hair >> 6) & 0x07);
        studio[0x1D] = (byte)(hair >> 9);
        studio[0x1B] = (byte)(hairColor == 0 ? 8 : hairColor);
        studio[0x1C] = (byte)((hair >> 5) & 1);

        var brow = ReadUInt32BigEndian(block, 0x24);
        var browColor = (int)((brow >> 13) & 0x07);
        studio[0x0E] = (byte)(brow >> 27);
        studio[0x0C] = (byte)((brow >> 22) & 0x0F);
        studio[0x0B] = (byte)(browColor == 0 ? 8 : browColor);
        studio[0x0D] = (byte)((brow >> 9) & 0x0F);
        studio[0x0A] = 3;
        studio[0x10] = (byte)((brow >> 4) & 0x1F);
        studio[0x0F] = (byte)(brow & 0x0F);

        var eye = ReadUInt32BigEndian(block, 0x28);
        studio[0x07] = (byte)(eye >> 26);
        studio[0x05] = (byte)((eye >> 21) & 0x07);
        studio[0x09] = (byte)((eye >> 16) & 0x1F);
        studio[0x04] = (byte)(((eye >> 13) & 0x07) + 8);
        studio[0x06] = (byte)((eye >> 9) & 0x07);
        studio[0x03] = 3;
        studio[0x08] = (byte)((eye >> 5) & 0x0F);

        var nose = ReadUInt16BigEndian(block, 0x2C);
        studio[0x2C] = (byte)(nose >> 12);
        studio[0x2B] = (byte)((nose >> 8) & 0x0F);
        studio[0x2D] = (byte)((nose >> 3) & 0x1F);

        var mouth = ReadUInt16BigEndian(block, 0x2E);
        var mouthColor = (int)((mouth >> 9) & 0x03);
        studio[0x26] = (byte)(mouth >> 11);
        studio[0x24] = (byte)(mouthColor + 19);
        studio[0x25] = (byte)((mouth >> 5) & 0x0F);
        studio[0x23] = 3;
        studio[0x27] = (byte)(mouth & 0x1F);

        var glasses = ReadUInt16BigEndian(block, 0x30);
        var glassesColor = (int)((glasses >> 9) & 0x07);
        studio[0x19] = (byte)(glasses >> 12);
        studio[0x17] = glassesColor switch
        {
            0 => 8,
            < 6 => (byte)(glassesColor + 13),
            _ => 0
        };
        studio[0x18] = (byte)((glasses >> 5) & 0x07);
        studio[0x1A] = (byte)(glasses & 0x1F);

        var facial = ReadUInt16BigEndian(block, 0x32);
        var facialHairColor = (int)((facial >> 9) & 0x07);
        studio[0x29] = (byte)(facial >> 14);
        studio[0x01] = (byte)((facial >> 12) & 0x03);
        studio[0x00] = (byte)(facialHairColor == 0 ? 8 : facialHairColor);
        studio[0x28] = (byte)((facial >> 5) & 0x0F);
        studio[0x2A] = (byte)(facial & 0x1F);

        var mole = ReadUInt16BigEndian(block, 0x34);
        studio[0x20] = (byte)(mole >> 15);
        studio[0x1F] = (byte)((mole >> 11) & 0x0F);
        studio[0x22] = (byte)((mole >> 6) & 0x1F);
        studio[0x21] = (byte)((mole >> 1) & 0x1F);

        return Encode(studio);
    }

    /// <summary>
    /// Obfuscates the Studio block the way the endpoint expects: a leading zero byte, then each
    /// byte XORed against the previous encoded one, offset by seven.
    /// </summary>
    private static string Encode(byte[] studioData)
    {
        var output = new StringBuilder("00", (studioData.Length + 1) * 2);
        byte rolling = 0;

        foreach (var value in studioData)
        {
            var encoded = (byte)((7 + (value ^ rolling)) & 0xFF);
            rolling = encoded;
            output.Append(encoded.ToString("x2", System.Globalization.CultureInfo.InvariantCulture));
        }

        return output.ToString();
    }

    private static ushort ReadUInt16BigEndian(byte[] bytes, int offset) =>
        (ushort)((bytes[offset] << 8) | bytes[offset + 1]);

    private static uint ReadUInt32BigEndian(byte[] bytes, int offset) =>
        ((uint)bytes[offset] << 24)
        | ((uint)bytes[offset + 1] << 16)
        | ((uint)bytes[offset + 2] << 8)
        | bytes[offset + 3];

    private static void WriteUInt16BigEndian(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)((value >> 8) & 0xFF);
        bytes[offset + 1] = (byte)(value & 0xFF);
    }
}
