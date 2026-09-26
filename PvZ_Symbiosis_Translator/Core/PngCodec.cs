using System;
using System.IO;
using System.IO.Compression;
using System.Text;
namespace PvZSymbiosisTranslator.Core;
// Deliberately supports non-interlaced RGB/RGBA 8-bit PNG only. Other formats fail safely.
public static class PngCodec
{
    public sealed class Pixels {public int Width,Height;public byte[] Rgba;}
    private static uint Read32(byte[] b,int i)=>(uint)(b[i]<<24|b[i+1]<<16|b[i+2]<<8|b[i+3]);
    private static void Write32(Stream s,uint v){s.WriteByte((byte)(v>>24));s.WriteByte((byte)(v>>16));s.WriteByte((byte)(v>>8));s.WriteByte((byte)v);}
    private static uint Crc(byte[] bytes){uint crc=0xffffffff;foreach(var b in bytes){crc^=b;for(int i=0;i<8;i++)crc=(crc>>1)^((crc&1)!=0?0xedb88320u:0);}return ~crc;}
    private static void Chunk(Stream s,string type,byte[] bytes){var t=Encoding.ASCII.GetBytes(type);Write32(s,(uint)bytes.Length);s.Write(t);s.Write(bytes);var all=new byte[t.Length+bytes.Length];t.CopyTo(all,0);bytes.CopyTo(all,4);Write32(s,Crc(all));}
    public static byte[] Encode(int width,int height,byte[] rgba)
    {
        if(width<=0||height<=0||rgba.Length!=checked(width*height*4))throw new FormatException("Invalid RGBA dimensions");
        using var output=new MemoryStream();output.Write(new byte[]{137,80,78,71,13,10,26,10});
        using var header=new MemoryStream();Write32(header,(uint)width);Write32(header,(uint)height);header.Write(new byte[]{8,6,0,0,0});Chunk(output,"IHDR",header.ToArray());
        using var compressed=new MemoryStream();using(var z=new ZLibStream(compressed,CompressionLevel.Optimal,true))for(int y=0;y<height;y++){z.WriteByte(0);z.Write(rgba,y*width*4,width*4);}
        Chunk(output,"IDAT",compressed.ToArray());Chunk(output,"IEND",Array.Empty<byte>());return output.ToArray();
    }
    public static Pixels Decode(byte[] png)
    {
        var signature=new byte[]{137,80,78,71,13,10,26,10};if(png.Length<33)throw new FormatException("Truncated PNG");for(int i=0;i<8;i++)if(png[i]!=signature[i])throw new FormatException("Invalid PNG");
        int width=0,height=0,bpp=0;bool ended=false;using var data=new MemoryStream();
        for(int offset=8;offset+12<=png.Length;){int count=checked((int)Read32(png,offset));if(count>png.Length-offset-12)throw new FormatException("Truncated chunk");var type=Encoding.ASCII.GetString(png,offset+4,4);var crcBytes=new byte[count+4];Array.Copy(png,offset+4,crcBytes,0,count+4);if(Crc(crcBytes)!=Read32(png,offset+8+count))throw new FormatException("PNG CRC mismatch");
            if(type=="IHDR"){width=checked((int)Read32(png,offset+8));height=checked((int)Read32(png,offset+12));if(count!=13||width<1||height<1||width>8192||height>8192||png[offset+16]!=8||png[offset+18]!=0||png[offset+19]!=0||png[offset+20]!=0)throw new FormatException("Unsupported PNG header");bpp=png[offset+17]==6?4:png[offset+17]==2?3:0;if(bpp==0)throw new FormatException("Use RGB/RGBA 8-bit PNG");}
            if(type=="tRNS")throw new FormatException("Use RGBA PNG for transparency");
            if(type=="IDAT")data.Write(png,offset+8,count);
            offset+=count+12;if(type=="IEND"){ended=true;break;}
        }
        if(!ended||bpp==0)throw new FormatException("Incomplete PNG");data.Position=0;using var z=new ZLibStream(data,CompressionMode.Decompress);
        int stride=checked(width*bpp);var result=new Pixels{Width=width,Height=height,Rgba=new byte[checked(width*height*4)]};var previous=new byte[stride];var current=new byte[stride];
        for(int y=0;y<height;y++){int filter=z.ReadByte();if(filter<0||filter>4)throw new FormatException("Invalid PNG filter");int read=0;while(read<stride){int n=z.Read(current,read,stride-read);if(n==0)throw new FormatException("Truncated pixels");read+=n;}
            for(int x=0;x<stride;x++){int a=x>=bpp?current[x-bpp]:0,b=previous[x],c=x>=bpp?previous[x-bpp]:0;int p=a+b-c;int pa=Math.Abs(p-a),pb=Math.Abs(p-b),pc=Math.Abs(p-c);int predictor=filter switch{0=>0,1=>a,2=>b,3=>(a+b)/2,4=>pa<=pb&&pa<=pc?a:pb<=pc?b:c,_=>0};current[x]=unchecked((byte)(current[x]+predictor));}
            for(int x=0;x<width;x++){int i=(y*width+x)*4;result.Rgba[i]=current[x*bpp];result.Rgba[i+1]=current[x*bpp+1];result.Rgba[i+2]=current[x*bpp+2];result.Rgba[i+3]=bpp==4?current[x*bpp+3]:(byte)255;}var swap=previous;previous=current;current=swap;
        }
        return result;
    }
}
