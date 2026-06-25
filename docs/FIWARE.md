# Integração FIWARE

O DriveTrace Core integra a plataforma **FIWARE** para representar o estado da fábrica
como **entidades de contexto NGSI-LD** e manter um histórico temporal desse contexto.
Isto aproxima o projeto de uma arquitetura de cidade/indústria inteligente e permite
interoperabilidade com outras ferramentas do ecossistema.

## Componentes

| Componente | Imagem | Função | Portas (host → Docker) |
|---|---|---|---|
| **Orion-LD** | `fiware/orion-ld` | Context Broker NGSI-LD: guarda o estado atual das entidades | `1026` |
| **IoT Agent JSON** | `fiware/iotagent-json` | Recebe medições em JSON e publica no Orion | API: `14041` → `4041` · payload/dispositivos: `7896` |
| **QuantumLeap** | `fiware/quantum-leap` | Subscreve o Orion e persiste o histórico temporal | `8668` |
| **MongoDB** | `mongo:4.4` | Armazenamento do Orion e do IoT Agent | `27017` |

> **Porta do IoT Agent**: dentro da rede Docker o IoT Agent continua a usar a porta
> `4041` (e `IOTA_PROVIDER_URL` permanece `http://iot-agent:4041`). No host (Windows) o
> acesso externo passou a ser feito por **`http://localhost:14041`**, porque a porta
> `4041` cai numa gama reservada pelo sistema operativo e impedia o container de arrancar.

## O que é publicado

A API mapeia o domínio da fábrica para entidades NGSI-LD, nomeadamente:

- **ProductionLine** e **Section** — linhas e secções de produção.
- **ProductUnit** — unidade de produto individual, com estado, qualidade e localização.
- **Support** — suporte associado à unidade (elemento central da rastreabilidade).
- **Rack** — armazenamento pós-linha.

À medida que as unidades avançam, mudam de qualidade ou são movimentadas, o contexto é
atualizado no Orion; o QuantumLeap recolhe a evolução para análise temporal (consumida
pelo Grafana).

## Verificação rápida

```bash
# Versão do Orion-LD (confirma que o broker está ativo)
curl http://localhost:1026/version

# Entidades atuais no broker (cabeçalhos NGSI-LD)
curl -H "NGSILD-Tenant: drivetrace" \
     -H "Accept: application/json" \
     "http://localhost:1026/ngsi-ld/v1/entities?type=ProductUnit"
```

> A coleção Postman em `docs/postman/` inclui pedidos de exemplo. Em Windows, o script
> `scripts/test-fiware.ps1` faz uma verificação automática da ponte FIWARE.

## Notas

- A ponte FIWARE é complementar à API REST: a aplicação funciona na mesma sem o Orion,
  mas perde a publicação de contexto e o histórico para o Grafana.
- O objetivo é demonstrar **rastreabilidade e contexto interoperável**, não substituir a
  base de dados relacional da aplicação.
