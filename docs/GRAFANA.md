# Monitorização e analítica (Grafana)

O **Grafana** fornece a camada de monitorização operacional do DriveTrace Core,
apresentando a evolução temporal da produção a partir do histórico de contexto
recolhido pelo **QuantumLeap** (ver [FIWARE.md](FIWARE.md)).

## Acesso

| Item | Valor |
|---|---|
| URL | http://localhost:33010 |
| Utilizador | `admin` |
| Palavra-passe | `admin` |

> No host, o Grafana é publicado na porta **33010** por defeito (mapeada para a `3000`
> interna do container). A porta externa pode ser ajustada com a variável `GRAFANA_PORT`.

A visualização anónima está ativa, pelo que os painéis podem também ser incorporados.

## O que é monitorizado

Indicadores operacionais ao longo do tempo, por exemplo:

- **WIP** (unidades em curso) por linha e por secção.
- **Qualidade**: aprovações, não conformidades, recondicionamento e sucata.
- **Fluxo**: movimentações entre secções e ocupação/gargalos.
- **Suportes e racks**: utilização pós-linha.

Enquanto o dashboard da aplicação mostra o estado **atual** e as decisões, o Grafana dá
a leitura **histórica e de tendência**, útil para análise de desempenho do turno.

## Fonte de dados

O Grafana lê o histórico que o QuantumLeap persiste a partir das entidades NGSI-LD do
Orion-LD. À medida que se gera atividade na aplicação (avançar unidades, decisões de
qualidade, movimentações), as séries temporais vão sendo preenchidas.

## Notas

- Se os painéis aparecerem vazios, é normal num arranque limpo: gere atividade na
  aplicação e aguarde a recolha do QuantumLeap.
- Não é necessário configurar nada manualmente para a demonstração; basta abrir o
  Grafana e percorrer os painéis.
