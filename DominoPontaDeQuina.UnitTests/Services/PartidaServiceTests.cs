using DominoPontaDeQuina.Application.Services;
using DominoPontaDeQuina.Domain.Entities;
using DominoPontaDeQuina.Domain.Repositories;

namespace DominoPontaDeQuina.UnitTests.Services;

/// <summary>
/// Testes do <see cref="PartidaService"/> com repositorios simulados pelo Moq.
/// Cada cenario prepara as respostas dos repositorios (Setup) e confere
/// as decisoes do servico de consultar (Obter/Existe) e salvar (Adicionar).
/// </summary>
[Trait("Categoria", "Servico")]
public class PartidaServiceTests
{
    readonly Mock<IPartidaRepository> _partidas = new(MockBehavior.Strict);
    readonly Mock<IJogadorRepository> _jogadores = new(MockBehavior.Strict);
    readonly Mock<IParticipacaoRepository> _participacoes = new(MockBehavior.Strict);
    readonly Mock<ILanceRepository> _lances = new(MockBehavior.Strict);
    readonly Mock<IRankingRepository> _ranking = new(MockBehavior.Strict);
    readonly PartidaService _sut;

    public PartidaServiceTests()
    {
        _sut = new PartidaService(
            _partidas.Object,
            _jogadores.Object,
            _participacoes.Object,
            _lances.Object,
            _ranking.Object);
    }

    static Partida CriarPartida(string status = "EmAndamento") => new()
    {
        Id = Guid.NewGuid(),
        PontuacaoAlvo = 50,
        Status = status
    };

    #region IniciarPartidaAsync

    [Fact(DisplayName = "Iniciar partida valida deve salvar a partida em andamento")]
    public async Task IniciarPartida_PontuacaoValida_DeveSalvarPartida()
    {
        // Arrange
        Partida? partidaSalva = null;
        _partidas
            .Setup(r => r.AdicionarAsync(It.IsAny<Partida>(), It.IsAny<CancellationToken>()))
            .Callback<Partida, CancellationToken>((p, _) => partidaSalva = p)
            .ReturnsAsync((Partida p, CancellationToken _) => p);

        // Act
        var resultado = await _sut.IniciarPartidaAsync(100);

        // Assert
        resultado.Should().BeSameAs(partidaSalva);
        resultado.PontuacaoAlvo.Should().Be(100);
        resultado.Status.Should().Be("EmAndamento");
        resultado.Id.Should().NotBeEmpty();
        _partidas.Verify(r => r.AdicionarAsync(It.IsAny<Partida>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory(DisplayName = "Pontuacao alvo invalida deve lancar excecao e nao salvar")]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task IniciarPartida_PontuacaoInvalida_DeveLancarExcecaoSemSalvar(int pontuacaoAlvo)
    {
        // Arrange
        var acao = () => _sut.IniciarPartidaAsync(pontuacaoAlvo);

        // Act + Assert
        await acao.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithParameterName("pontuacaoAlvo");
        _partidas.Verify(r => r.AdicionarAsync(It.IsAny<Partida>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region RegistrarJogadorAsync

    [Fact(DisplayName = "Registrar jogador em partida existente deve consultar a partida e salvar jogador e participacao")]
    public async Task RegistrarJogador_PartidaExistente_DeveSalvarJogadorEParticipacao()
    {
        // Arrange
        var partida = CriarPartida();
        var usuarioId = Guid.NewGuid();
        _partidas
            .Setup(r => r.ObterAsync(partida.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(partida);
        _jogadores
            .Setup(r => r.AdicionarComUsuarioAsync(It.IsAny<Jogador>(), usuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Jogador j, Guid? _, CancellationToken _) => j);
        _participacoes
            .Setup(r => r.AdicionarNaPartidaAsync(It.IsAny<ParticipacaoPartida>(), partida.Id, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var jogador = await _sut.RegistrarJogadorAsync(partida.Id, "Ana", usuarioId);

        // Assert
        jogador.Nome.Should().Be("Ana");
        _partidas.Verify(r => r.ObterAsync(partida.Id, It.IsAny<CancellationToken>()), Times.Once);
        _jogadores.Verify(r => r.AdicionarComUsuarioAsync(
            It.Is<Jogador>(j => j.Nome == "Ana"), usuarioId, It.IsAny<CancellationToken>()), Times.Once);
        _participacoes.Verify(r => r.AdicionarNaPartidaAsync(
            It.Is<ParticipacaoPartida>(p => p.Pontos == 0), partida.Id, jogador.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "Registrar jogador em partida inexistente deve lancar KeyNotFoundException e nao salvar")]
    public async Task RegistrarJogador_PartidaInexistente_DeveLancarExcecaoSemSalvar()
    {
        // Arrange
        var partidaId = Guid.NewGuid();
        _partidas
            .Setup(r => r.ObterAsync(partidaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Partida?)null);

        // Act
        var acao = () => _sut.RegistrarJogadorAsync(partidaId, "Ana");

        // Assert
        await acao.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Partida não encontrada.");
        _jogadores.Verify(r => r.AdicionarComUsuarioAsync(It.IsAny<Jogador>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
        _participacoes.Verify(r => r.AdicionarNaPartidaAsync(It.IsAny<ParticipacaoPartida>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory(DisplayName = "Nome invalido deve lancar ArgumentException sem consultar nem salvar")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RegistrarJogador_NomeInvalido_NaoDeveConsultarNemSalvar(string nome)
    {
        // Arrange
        var partidaId = Guid.NewGuid();

        // Act
        var acao = () => _sut.RegistrarJogadorAsync(partidaId, nome);

        // Assert
        await acao.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("nome");
        _partidas.Verify(r => r.ObterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _jogadores.VerifyNoOtherCalls();
        _participacoes.VerifyNoOtherCalls();
    }

    #endregion

    #region RegistrarLanceAsync

    [Fact(DisplayName = "Lance valido deve consultar partida e jogador e salvar o lance")]
    public async Task RegistrarLance_PartidaEmAndamentoEJogadorExistente_DeveSalvarLance()
    {
        // Arrange
        var partida = CriarPartida();
        var jogadorId = Guid.NewGuid();
        _partidas
            .Setup(r => r.ObterAsync(partida.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(partida);
        _jogadores
            .Setup(r => r.ExisteAsync(jogadorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _lances
            .Setup(r => r.AdicionarNaPartidaAsync(It.IsAny<Lance>(), partida.Id, jogadorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lance l, Guid _, Guid _, CancellationToken _) => l);

        // Act
        var lance = await _sut.RegistrarLanceAsync(partida.Id, jogadorId);

        // Assert
        lance.Should().NotBeNull();
        lance.Id.Should().NotBeEmpty();
        lance.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        _jogadores.Verify(r => r.ExisteAsync(jogadorId, It.IsAny<CancellationToken>()), Times.Once);
        _lances.Verify(r => r.AdicionarNaPartidaAsync(It.IsAny<Lance>(), partida.Id, jogadorId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory(DisplayName = "Lance em partida fora de andamento deve lancar InvalidOperationException e nao salvar")]
    [InlineData("Finalizada")]
    [InlineData("Cancelada")]
    public async Task RegistrarLance_PartidaNaoEmAndamento_DeveLancarExcecaoSemSalvar(string status)
    {
        // Arrange
        var partida = CriarPartida(status);
        _partidas
            .Setup(r => r.ObterAsync(partida.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(partida);

        // Act
        var acao = () => _sut.RegistrarLanceAsync(partida.Id, Guid.NewGuid());

        // Assert
        await acao.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*não está em andamento*");
        _jogadores.Verify(r => r.ExisteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _lances.Verify(r => r.AdicionarNaPartidaAsync(It.IsAny<Lance>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "Lance de jogador inexistente deve lancar KeyNotFoundException e nao salvar")]
    public async Task RegistrarLance_JogadorInexistente_DeveLancarExcecaoSemSalvar()
    {
        // Arrange
        var partida = CriarPartida();
        var jogadorId = Guid.NewGuid();
        _partidas
            .Setup(r => r.ObterAsync(partida.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(partida);
        _jogadores
            .Setup(r => r.ExisteAsync(jogadorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var acao = () => _sut.RegistrarLanceAsync(partida.Id, jogadorId);

        // Assert
        await acao.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Jogador não encontrado.");
        _lances.Verify(r => r.AdicionarNaPartidaAsync(It.IsAny<Lance>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Consultas

    [Fact(DisplayName = "Verificar status deve retornar a partida consultada no repositorio")]
    public async Task VerificarStatus_PartidaExistente_DeveRetornarPartida()
    {
        // Arrange
        var partida = CriarPartida("Finalizada");
        _partidas
            .Setup(r => r.ObterAsync(partida.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(partida);

        // Act
        var resultado = await _sut.VerificarStatusAsync(partida.Id);

        // Assert
        resultado.Should().BeSameAs(partida);
        resultado.Status.Should().Be("Finalizada");
        _partidas.Verify(r => r.ObterAsync(partida.Id, It.IsAny<CancellationToken>()), Times.Once);
        _partidas.VerifyNoOtherCalls();
    }

    [Fact(DisplayName = "Verificar status de partida inexistente deve lancar KeyNotFoundException")]
    public async Task VerificarStatus_PartidaInexistente_DeveLancarExcecao()
    {
        // Arrange
        var partidaId = Guid.NewGuid();
        _partidas
            .Setup(r => r.ObterAsync(partidaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Partida?)null);

        // Act
        var acao = () => _sut.VerificarStatusAsync(partidaId);

        // Assert
        await acao.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact(DisplayName = "Consultar ranking deve repassar o resultado do repositorio sem gravar nada")]
    public async Task ConsultarRanking_DeveRetornarRankingDoRepositorio()
    {
        // Arrange
        IReadOnlyList<Ranking> rankingEsperado =
        [
            new Ranking { Vitorias = 5, Jogador = new Jogador { Nome = "Ana" } },
            new Ranking { Vitorias = 3, Jogador = new Jogador { Nome = "Bia" } }
        ];
        _ranking
            .Setup(r => r.ObterAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rankingEsperado);

        // Act
        var resultado = await _sut.ConsultarRankingAsync();

        // Assert
        resultado.Should().BeEquivalentTo(rankingEsperado, o => o.WithStrictOrdering());
        resultado.Select(r => r.Vitorias).Should().BeInDescendingOrder();
        _ranking.Verify(r => r.ObterAsync(It.IsAny<CancellationToken>()), Times.Once);
        _ranking.VerifyNoOtherCalls();
    }

    [Fact(DisplayName = "Consultar historico deve repassar as partidas do repositorio")]
    public async Task ConsultarHistorico_DeveRetornarPartidasDoRepositorio()
    {
        // Arrange
        IReadOnlyList<Partida> historico = [CriarPartida("Finalizada"), CriarPartida()];
        _partidas
            .Setup(r => r.ObterHistoricoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(historico);

        // Act
        var resultado = await _sut.ConsultarHistoricoAsync();

        // Assert
        resultado.Should().HaveCount(2).And.ContainInOrder(historico);
        _partidas.Verify(r => r.ObterHistoricoAsync(It.IsAny<CancellationToken>()), Times.Once);
        _partidas.VerifyNoOtherCalls();
    }

    #endregion
}
