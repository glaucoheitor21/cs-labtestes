namespace DominoPontaDeQuina.Core.Regras;

/// <summary>
/// Concentra a regra de pontuacao do Domino Ponta de Quina.
/// Apos cada jogada, a soma das pontas externas do tabuleiro gera pontos quando for multipla de 5:
/// cada multiplo de 5 vale 1 ponto (soma / 5). Somas que nao sao multiplas de 5 nao pontuam.
/// </summary>
public static class CalculadoraPontuacao
{
    /// <summary>
    /// Valor base da regra: somente somas multiplas deste numero pontuam.
    /// </summary>
    public const int Divisor = 5;

    /// <summary>
    /// Calcula os pontos obtidos a partir da soma das pontas externas.
    /// </summary>
    /// <param name="somaPontas">A soma das pontas externas do tabuleiro.</param>
    /// <returns><c>somaPontas / 5</c> quando a soma for multipla de 5; caso contrario, 0.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Quando a soma for negativa.</exception>
    public static int Calcular(int somaPontas)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(somaPontas);

        return somaPontas % Divisor == 0 ? somaPontas / Divisor : 0;
    }
}
