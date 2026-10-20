using DominoPontaDeQuina.Core.Models;
using DominoPontaDeQuina.Core.Regras;

namespace DominoPontaDeQuina.UnitTests.Pontuacao;

/// <summary>
/// Testes da regra de pontuacao: somas multiplas de 5 geram soma / 5 pontos.
/// </summary>
[Trait("Categoria", "Pontuacao")]
public class CalculadoraPontuacaoTests
{
    [Fact(DisplayName = "Soma 10 deve gerar 2 pontos")]
    public void Calcular_Soma10_DeveRetornar2()
    {
        // Arrange
        var soma = 10;

        // Act
        var pontos = CalculadoraPontuacao.Calcular(soma);

        // Assert
        pontos.Should().Be(2);
    }

    [Fact(DisplayName = "Soma 15 deve gerar 3 pontos")]
    public void Calcular_Soma15_DeveRetornar3()
    {
        // Arrange
        var soma = 15;

        // Act
        var pontos = CalculadoraPontuacao.Calcular(soma);

        // Assert
        pontos.Should().Be(3);
    }

    [Theory(DisplayName = "Somas multiplas de 5 devem gerar soma / 5 pontos")]
    [InlineData(5, 1)]
    [InlineData(10, 2)]
    [InlineData(15, 3)]
    [InlineData(20, 4)]
    [InlineData(25, 5)]
    public void Calcular_SomaMultiplaDe5_DeveRetornarSomaDivididaPor5(int soma, int pontosEsperados)
    {
        // Arrange (dados vindos do InlineData)

        // Act
        var pontos = CalculadoraPontuacao.Calcular(soma);

        // Assert
        pontos.Should().Be(pontosEsperados);
    }

    [Theory(DisplayName = "Somas que nao sao multiplas de 5 nao pontuam")]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(7)]
    [InlineData(11)]
    [InlineData(23)]
    public void Calcular_SomaNaoMultiplaDe5_DeveRetornarZero(int soma)
    {
        // Arrange (dados vindos do InlineData)

        // Act
        var pontos = CalculadoraPontuacao.Calcular(soma);

        // Assert
        pontos.Should().Be(0);
    }

    [Fact(DisplayName = "Soma 0 (tabuleiro vazio) nao pontua")]
    public void Calcular_SomaZero_DeveRetornarZero()
    {
        // Arrange
        var soma = 0;

        // Act
        var pontos = CalculadoraPontuacao.Calcular(soma);

        // Assert
        pontos.Should().Be(0);
    }

    [Theory(DisplayName = "Soma negativa deve lancar ArgumentOutOfRangeException")]
    [InlineData(-1)]
    [InlineData(-5)]
    public void Calcular_SomaNegativa_DeveLancarExcecao(int soma)
    {
        // Arrange
        var acao = () => CalculadoraPontuacao.Calcular(soma);

        // Act + Assert
        acao.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("somaPontas");
    }

    [Theory(DisplayName = "Tabuleiro deve pontuar pela soma das pontas externas")]
    [InlineData(4, 6, 2)]  // pontas 4 e 6 -> soma 10 -> 2 pontos
    [InlineData(5, 0, 1)]  // pontas 5 e 0 -> soma 5  -> 1 ponto
    [InlineData(6, 3, 0)]  // pontas 6 e 3 -> soma 9  -> nao pontua
    public void Tabuleiro_CalcularPontuacao_DeveUsarPontasExternas(int pontaEsquerda, int pontaDireita, int pontosEsperados)
    {
        // Arrange
        var tabuleiro = new Tabuleiro();
        tabuleiro.Pecas.Add(new Peca(pontaEsquerda, 2));
        tabuleiro.Pecas.Add(new Peca(2, pontaDireita));

        // Act
        var pontos = tabuleiro.CalcularPontuacao();

        // Assert
        tabuleiro.SomarPontasExternas().Should().Be(pontaEsquerda + pontaDireita);
        pontos.Should().Be(pontosEsperados);
    }
}
