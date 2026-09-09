using GeneFlow.ApiNet2.Domain.Traces.Enumerations;

namespace GeneFlow.ApiNet2.Tests.Domain.Traces.Enumerations;

/// <summary>
/// Unit tests for TraceFormat enumeration.
/// </summary>
public class TraceFormatTests
{
    #region Format Values

    [Fact]
    public void AllFormats_ShouldHaveUniqueIds()
    {
        // Arrange
        var formats = TraceFormat.GetAll();

        // Act
        var ids = formats.Select(f => f.Id).ToList();

        // Assert
        ids.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void AllFormats_ShouldHaveUniqueNames()
    {
        // Arrange
        var formats = TraceFormat.GetAll();

        // Act
        var names = formats.Select(f => f.Name).ToList();

        // Assert
        names.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void AB1_ShouldHaveCorrectProperties()
    {
        // Assert
        TraceFormat.AB1.Id.Should().Be(1);
        TraceFormat.AB1.Name.Should().Be("AB1");
        TraceFormat.AB1.DisplayName.Should().Be("Applied Biosystems");
        TraceFormat.AB1.Extension.Should().Be(".ab1");
        TraceFormat.AB1.HasChromatogramData.Should().BeTrue();
    }

    [Fact]
    public void SCF_ShouldHaveCorrectProperties()
    {
        // Assert
        TraceFormat.SCF.Id.Should().Be(2);
        TraceFormat.SCF.Name.Should().Be("SCF");
        TraceFormat.SCF.DisplayName.Should().Be("Standard Chromatogram Format");
        TraceFormat.SCF.Extension.Should().Be(".scf");
        TraceFormat.SCF.HasChromatogramData.Should().BeTrue();
    }

    [Fact]
    public void FASTQ_ShouldHaveCorrectProperties()
    {
        // Assert
        TraceFormat.FASTQ.Id.Should().Be(3);
        TraceFormat.FASTQ.Name.Should().Be("FASTQ");
        TraceFormat.FASTQ.DisplayName.Should().Be("FASTQ Format");
        TraceFormat.FASTQ.Extension.Should().Be(".fastq");
        TraceFormat.FASTQ.HasChromatogramData.Should().BeFalse();
    }

    [Fact]
    public void FASTA_ShouldHaveCorrectProperties()
    {
        // Assert
        TraceFormat.FASTA.Id.Should().Be(4);
        TraceFormat.FASTA.Name.Should().Be("FASTA");
        TraceFormat.FASTA.DisplayName.Should().Be("FASTA Format");
        TraceFormat.FASTA.Extension.Should().Be(".fasta");
        TraceFormat.FASTA.HasChromatogramData.Should().BeFalse();
    }

    [Fact]
    public void GenBank_ShouldHaveCorrectProperties()
    {
        // Assert
        TraceFormat.GenBank.Id.Should().Be(5);
        TraceFormat.GenBank.Name.Should().Be("GenBank");
        TraceFormat.GenBank.DisplayName.Should().Be("GenBank Format");
        TraceFormat.GenBank.Extension.Should().Be(".gb");
        TraceFormat.GenBank.HasChromatogramData.Should().BeFalse();
    }

    #endregion

    #region FromExtension

    [Theory]
    [InlineData(".ab1", "AB1")]
    [InlineData(".AB1", "AB1")]
    [InlineData("ab1", "AB1")]
    [InlineData(".scf", "SCF")]
    [InlineData(".SCF", "SCF")]
    [InlineData("scf", "SCF")]
    [InlineData(".fastq", "FASTQ")]
    [InlineData(".fq", "FASTQ")]
    [InlineData("fastq", "FASTQ")]
    [InlineData(".fasta", "FASTA")]
    [InlineData(".fa", "FASTA")]
    [InlineData("fasta", "FASTA")]
    [InlineData(".gb", "GenBank")]
    [InlineData(".gbk", "GenBank")]
    [InlineData(".genbank", "GenBank")]
    public void FromExtension_WithValidExtension_ShouldReturnCorrectFormat(string extension, string expectedName)
    {
        // Act
        var format = TraceFormat.FromExtension(extension);

        // Assert
        format.Should().NotBeNull();
        format!.Name.Should().Be(expectedName);
    }

    [Theory]
    [InlineData(".txt")]
    [InlineData(".pdf")]
    [InlineData(".doc")]
    [InlineData(".unknown")]
    [InlineData("xyz")]
    public void FromExtension_WithInvalidExtension_ShouldReturnNull(string extension)
    {
        // Act
        var format = TraceFormat.FromExtension(extension);

        // Assert
        format.Should().BeNull();
    }

    [Fact]
    public void FromExtension_IsCaseInsensitive()
    {
        // Act
        var lower = TraceFormat.FromExtension(".ab1");
        var upper = TraceFormat.FromExtension(".AB1");
        var mixed = TraceFormat.FromExtension(".Ab1");

        // Assert
        lower.Should().Be(upper);
        upper.Should().Be(mixed);
    }

    #endregion

    #region GetContentType

    [Fact]
    public void AB1_GetContentType_ShouldReturnOctetStream()
    {
        TraceFormat.AB1.GetContentType().Should().Be("application/octet-stream");
    }

    [Fact]
    public void SCF_GetContentType_ShouldReturnOctetStream()
    {
        TraceFormat.SCF.GetContentType().Should().Be("application/octet-stream");
    }

    [Fact]
    public void FASTQ_GetContentType_ShouldReturnTextPlain()
    {
        TraceFormat.FASTQ.GetContentType().Should().Be("text/plain");
    }

    [Fact]
    public void FASTA_GetContentType_ShouldReturnTextPlain()
    {
        TraceFormat.FASTA.GetContentType().Should().Be("text/plain");
    }

    [Fact]
    public void GenBank_GetContentType_ShouldReturnTextPlain()
    {
        TraceFormat.GenBank.GetContentType().Should().Be("text/plain");
    }

    #endregion

    #region HasChromatogramData

    [Fact]
    public void ChromatogramFormats_ShouldHaveChromatogramData()
    {
        // Only AB1 and SCF have chromatogram data
        TraceFormat.AB1.HasChromatogramData.Should().BeTrue();
        TraceFormat.SCF.HasChromatogramData.Should().BeTrue();
    }

    [Fact]
    public void SequenceOnlyFormats_ShouldNotHaveChromatogramData()
    {
        TraceFormat.FASTQ.HasChromatogramData.Should().BeFalse();
        TraceFormat.FASTA.HasChromatogramData.Should().BeFalse();
        TraceFormat.GenBank.HasChromatogramData.Should().BeFalse();
    }

    #endregion

    #region FromId

    [Theory]
    [InlineData(1, "AB1")]
    [InlineData(2, "SCF")]
    [InlineData(3, "FASTQ")]
    [InlineData(4, "FASTA")]
    [InlineData(5, "GenBank")]
    public void FromId_ShouldReturnCorrectFormat(int id, string expectedName)
    {
        // Act
        var format = TraceFormat.FromId(id);

        // Assert
        format.Name.Should().Be(expectedName);
    }

    #endregion

    #region FromName

    [Theory]
    [InlineData("AB1", 1)]
    [InlineData("SCF", 2)]
    [InlineData("FASTQ", 3)]
    [InlineData("FASTA", 4)]
    [InlineData("GenBank", 5)]
    public void FromName_ShouldReturnCorrectFormat(string name, int expectedId)
    {
        // Act
        var format = TraceFormat.FromName(name);

        // Assert
        format.Id.Should().Be(expectedId);
    }

    #endregion

    #region List

    [Fact]
    public void List_ShouldContainAllFormats()
    {
        // Act
        var formats = TraceFormat.GetAll();

        // Assert
        formats.Should().HaveCount(5);
        formats.Should().Contain(TraceFormat.AB1);
        formats.Should().Contain(TraceFormat.SCF);
        formats.Should().Contain(TraceFormat.FASTQ);
        formats.Should().Contain(TraceFormat.FASTA);
        formats.Should().Contain(TraceFormat.GenBank);
    }

    #endregion
}
