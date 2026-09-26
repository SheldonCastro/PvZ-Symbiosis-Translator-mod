# Guia de estilo PT-BR

Este guia define as escolhas canônicas para a tradução de PvZ Symbiosis. O source chinês, os valores mecânicos e as tags nunca são normalizados ou reescritos.

## Tom e capitalização

- Botões e comandos usam sentence case: `Iniciar jogo`, `Comprar agora`, `Ver plantas`, `Voltar`.
- Modos e recursos nomeados usam maiúscula de nome próprio: `Modo Aventura`, `Modo Desafio`.
- Frases descritivas usam capitalização normal do português.
- Nomes canônicos de entidades mantêm maiúscula: `Disparervilha`, `Zumbi Cone`, `Cereja-Bomba`.
- Handles, autores e nomes de colaboradores permanecem na forma original.

## Mecânicas

| Chinês | PT-BR canônico | Uso |
|---|---|---|
| 阳光 | Sol | Recurso/moeda; `sol` minúsculo apenas para o astro em prosa. |
| 伤害 | dano | Minúsculo em frases; `Dano` em rótulos. |
| 生命值 | vida | Preferido a “pontos de vida” quando o controle for compacto. |
| 韧性 | resistência | Atributo defensivo exibido pelo jogo. |
| 升级 | aprimoramento / aprimorar | Nome do sistema e ação. “Melhoria” só em prosa geral. |
| 冷却 / 冷却时间 | recarga / tempo de recarga | Nunca `cooldown`. |
| 范围伤害 | dano em área | Forma canônica. |
| 路径 | caminho | Estrutura principal/secundária da interface. |
| 技能 | habilidade | Habilidade ativa ou passiva conforme o contexto. |
| 关卡 | fase | Nível jogável. |
| 暴走 | fúria / furioso | Estado e variante da entidade. |
| 强化 | fortalecimento / fortalecido | Ação e variante da entidade. |

## Rotas

- Famílias `NAME：其一/其二/其三` usam `NAME: Rota um`, `Rota dois`, `Rota três`.
- As opções genéricas `其一/其二/其三` usam `Opção um`, `Opção dois`, `Opção três`.
- `caminho principal` e `caminho secundário` descrevem a estrutura da interface e não substituem o nome de uma rota.

## Números e unidades

- Decimais de texto exibido usam vírgula: `2,5 s`, `1,833 s`.
- Versões de software preservam pontos: `1.2.0`.
- Multiplicação visual usa `×`: `20 × 4`.
- Intervalos compactos usam `a cada`: `20 de dano a cada 1,666 s`.
- Segundos usam `s` em estatísticas e cards.
- Níveis abreviados usam algarismos: `Nv. 1`, `Nv. 2`, `Nv. 3`.
- Valores e probabilidades nunca são convertidos ou arredondados.

## Rich text e placeholders

- Tags TMP precisam permanecer estruturalmente balanceadas.
- A formatação pode mudar apenas quando necessária para legibilidade; ela não pode alterar placeholders ou mecânicas.
- Placeholders precisam manter nome, quantidade e multiplicidade.
- Créditos em `<align=right>` podem manter CJK intencionalmente.

## Nomes canônicos do projeto

- 豌豆射手 — Disparervilha
- 双发豌豆 — Duplervilha
- 机枪豌豆 — Metralhervilha
- 寒冰射手 — Gelervilha
- 裂荚射手 — Bidisparervilha
- 坚果墙 — Noz-Parede
- 大嘴花 — Mastigadora
- 小喷菇 — Mini-Cogumelo

As alternativas do projeto Fusion são consultivas. O nome de Symbiosis prevalece quando identifica uma entidade ou evolução específica deste jogo.
