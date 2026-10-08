using Pikwise.Application.Explanations.Exceptions;
using Pikwise.Application.Explanations.Models;
using Pikwise.Application.Explanations.Validation;
using Pikwise.Application.Products.DTOs;
using Pikwise.Application.Recommendations.DTOs;

namespace Pikwise.UnitTests;

public class ExplanationFactCheckerTests
{
    // Numbers in this input: 7, 1, 25, 17, 68000, 16, 15.6, 1920, 1080, 1.55, 11, 75.41, 12.5 and the "15" in X15.
    private static ExplanationInput Input(ValueAnalysisDto? valueAnalysis = null) => new(
        new RecommendationRequestDto(),
        25,
        17,
        [
            new ExplanationProduct(1, 7, "Test Laptop X15", "Test", 68000.00m,
                new LaptopSpecificationDto("Test CPU", null, 16, null, 15.6m, "1920x1080", null, 1.55m, "Windows 11"),
                75.41m, ["gpu"], [new ExplanationScoreComponent("ram", 16m, 12.5m)])
        ],
        valueAnalysis);

    [Theory]
    [InlineData("Fiyatı 68.000 TL.")]
    [InlineData("Fiyatı 68000 TL.")]
    [InlineData("Fiyatı 68,000 TL.")]
    [InlineData("Fiyatı 68 bin TL.")]
    [InlineData("Ağırlığı 1,55 kg.")]
    [InlineData("Ağırlığı 1.55 kg.")]
    [InlineData("Ağırlığı yaklaşık 1,6 kg.")]
    [InlineData("75,41 puan, yani yaklaşık 75 puan.")]
    [InlineData("16 GB RAM puana 12,5 puan katkı veriyor.")]
    [InlineData("15,6 inç 1920x1080 ekran, Windows 11.")]
    [InlineData("Test Laptop X15, 17 uygun ürün arasında 1. sırada.")]
    [InlineData("Ekran kartı bilgisi yok.")]
    public void Text_using_only_input_numbers_passes(string text)
    {
        ExplanationFactChecker.Check(Output(text), Input(), Json(Input()));
    }

    [Theory]
    [InlineData("Fiyatı 70.000 TL.")]
    [InlineData("Diğerlerinden %10 daha ucuz.")]
    [InlineData("32 GB RAM'e yükseltilebilir.")]
    [InlineData("Pil ömrü 10 saat.")]
    [InlineData("4K ekranı var.")]
    [InlineData("2024 modeli.")]
    [InlineData("Fiyatı 69 bin TL.")]
    public void Text_with_a_number_not_in_the_input_is_rejected(string text)
    {
        Assert.Throws<ExplanationInvalidException>(() => ExplanationFactChecker.Check(Output(text), Input(), Json(Input())));
    }

    [Fact]
    public void Missing_extra_duplicate_or_null_product_entries_are_rejected()
    {
        var input = Input();
        var json = Json(input);
        GeneratedExplanation With(params GeneratedProductExplanation?[] items) => new() { Products = items };
        var valid = new GeneratedProductExplanation { ProductId = 7, Explanation = "Uygun." };

        Assert.Throws<ExplanationInvalidException>(() => ExplanationFactChecker.Check(new GeneratedExplanation(), input, json));
        Assert.Throws<ExplanationInvalidException>(() => ExplanationFactChecker.Check(With(), input, json));
        Assert.Throws<ExplanationInvalidException>(() => ExplanationFactChecker.Check(With((GeneratedProductExplanation?)null), input, json));
        Assert.Throws<ExplanationInvalidException>(() => ExplanationFactChecker.Check(
            With(new GeneratedProductExplanation { ProductId = 8, Explanation = "Uygun." }), input, json));
        Assert.Throws<ExplanationInvalidException>(() => ExplanationFactChecker.Check(With(valid, valid), input, json));
        Assert.Throws<ExplanationInvalidException>(() => ExplanationFactChecker.Check(
            With(valid, new GeneratedProductExplanation { ProductId = 8, Explanation = "Uygun." }), input, json));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Empty_explanation_is_rejected(string? text)
    {
        Assert.Throws<ExplanationInvalidException>(() => ExplanationFactChecker.Check(Output(text), Input(), Json(Input())));
    }

    [Fact]
    public void Too_long_explanation_is_rejected()
    {
        var text = new string('a', ExplanationFactChecker.MaxTextLength + 1);

        Assert.Throws<ExplanationInvalidException>(() => ExplanationFactChecker.Check(Output(text), Input(), Json(Input())));
    }

    [Fact]
    public void Value_comment_is_rejected_without_a_value_analysis_and_checked_when_one_exists()
    {
        var analysis = new ValueAnalysisDto(7, 7, false, 3m, []);

        Assert.Throws<ExplanationInvalidException>(() =>
            ExplanationFactChecker.Check(Output("Uygun.", "Fiyatına değer."), Input(), Json(Input())));
        ExplanationFactChecker.Check(Output("Uygun.", "3 puanlık farkta daha ucuz bir seçenek yok."), Input(analysis), Json(Input(analysis)));
        Assert.Throws<ExplanationInvalidException>(() =>
            ExplanationFactChecker.Check(Output("Uygun.", "5.000 TL fazlasına değer."), Input(analysis), Json(Input(analysis))));
    }

    private static GeneratedExplanation Output(string? text, string? valueComment = null) => new()
    {
        Products = [new GeneratedProductExplanation { ProductId = 7, Explanation = text }],
        ValueComment = valueComment
    };

    private static string Json(ExplanationInput input) => ExplanationContract.SerializeInput(input);
}
