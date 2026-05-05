import { ref, watch } from 'vue'

export const locale = ref<string>(localStorage.getItem('locale') || 'pt-PT')
export const theme = ref<string>(localStorage.getItem('theme') || 'light')

function applyTheme(value: string) {
  if (value === 'dark') document.documentElement.classList.add('dark')
  else document.documentElement.classList.remove('dark')
}

applyTheme(theme.value)

watch(locale, (value) => localStorage.setItem('locale', value))
watch(theme, (value) => {
  localStorage.setItem('theme', value)
  applyTheme(value)
})

export function toggleTheme(): void {
  theme.value = theme.value === 'light' ? 'dark' : 'light'
}

export function setLocale(newLocale: string): void {
  if (newLocale === 'pt-PT' || newLocale === 'en') locale.value = newLocale
}

const pt: Record<string, string> = {
  'Dashboard / Line Overview': 'Painel / Visão da Linha',
  'Manufacturing Orders': 'Ordens de Fabrico',
  'Product Units': 'Unidades de Produto',
  'Supports / WIP Tracking': 'Suportes / Rastreabilidade WIP',
  'Materials and Lots': 'Materiais e Lotes',
  'Quality': 'Qualidade',
  'Racks / Post-line Logistics': 'Racks / Logística Pós-Linha',
  'Event Playback': 'Reprodução de Eventos',
  'FIWARE Context Monitor': 'Monitor de Contexto FIWARE',
  'Users': 'Utilizadores',
  'User Management': 'Gestão de Utilizadores',
  'Predictions': 'Previsões',
  'Automotive WIP traceability': 'Rastreabilidade WIP automóvel',
  'DriveTrace Core monitors product units through their physical supports.': 'O DriveTrace Core monitoriza unidades de produto através dos seus suportes físicos.',
  'The V1 uses a controlled door production line: raw materials, support assignment, stamping, welding, painting, quality control and post-line rack storage.': 'A V1 utiliza uma linha de produção de portas controlada: matérias-primas, atribuição de suportes, estampagem, soldadura, pintura, controlo de qualidade e armazenamento em racks pós-linha.',
  'ProductUnit-centred traceability': 'Rastreabilidade centrada na unidade de produto',
  'Support as intra-line anchor': 'Suporte como âncora intra-linha',
  'Rack as post-line logistics': 'Rack como logística pós-linha',
  'Material lot genealogy': 'Genealogia de lotes de matéria-prima',
  'Open orders': 'Ordens abertas',
  'Active units': 'Unidades ativas',
  'Active supports': 'Suportes ativos',
  'Quality issues': 'Problemas de qualidade',
  'Rack assignments': 'Atribuições a racks',
  'Line state': 'Estado da linha',
  'Current WIP by section': 'WIP atual por secção',
  'Open alerts and deviations': 'Alertas e desvios abertos',
  'Recent events': 'Eventos recentes',
  'Support movement and audit trail': 'Movimento dos suportes e auditoria',
  'Planning': 'Planeamento',
  'Manufacturing orders': 'Ordens de fabrico',
  'Order': 'Ordem',
  'Planned qty': 'Quantidade planeada',
  'Scheduled until': 'Agendado até',
  'Notes': 'Notas',
  'Traceable units': 'Unidades rastreáveis',
  'Active / held': 'Ativas / retidas',
  'Deviations': 'Desvios',
  'Unitary traceability': 'Rastreabilidade unitária',
  'Product units and subproducts': 'Unidades de produto e subprodutos',
  'Unit': 'Unidade',
  'Type': 'Tipo',
  'Status': 'Estado',
  'Current support': 'Suporte atual',
  'Current section': 'Secção atual',
  'Physical tracking': 'Rastreamento físico',
  'Support is the intra-line anchor': 'O suporte é a âncora intra-linha',
  'Items and lots': 'Materiais e lotes',
  'Raw materials': 'Matérias-primas',
  'Genealogy': 'Genealogia',
  'Material lots': 'Lotes de matéria-prima',
  'Lot': 'Lote',
  'Material': 'Material',
  'Quantity': 'Quantidade',
  'Section': 'Secção',
  'Results': 'Resultados',
  'PASS': 'Aprovado',
  'FAIL': 'Reprovado',
  'Quality evidence': 'Evidências de qualidade',
  'Results, nonconformities, rework and scrap': 'Resultados, não conformidades, retrabalho e sucata',
  'Recorded at': 'Registado em',
  'Post-line logistics': 'Logística pós-linha',
  'Racks are not the WIP anchor': 'As racks não são a âncora do WIP',
  'Extended view': 'Vista alargada',
  'Subproduct to final assembly concept': 'Conceito de subproduto para montagem final',
  'Simulation': 'Simulação',
  'Execute event playback': 'Executar reprodução de eventos',
  'Execute playback scenario': 'Executar cenário de reprodução',
  'Controlled event': 'Evento controlado',
  'Inject manual factory event': 'Injetar evento manual de fábrica',
  'Event type': 'Tipo de evento',
  'Support code': 'Código do suporte',
  'Section / Rack code': 'Código da secção / rack',
  'Unit code': 'Código da unidade',
  'Result': 'Resultado',
  'Inject manual event': 'Injetar evento manual',
  'Context broker boundary': 'Fronteira do broker de contexto',
  'Current NGSI-LD-style context': 'Contexto atual estilo NGSI-LD',
  'Publish current context to Orion-LD': 'Publicar contexto atual no Orion-LD',
  'Event': 'Evento',
  'Support': 'Suporte',
  'Timestamp': 'Marca temporal',
  'Refresh': 'Atualizar',
  'Profile': 'Perfil',
  'Settings': 'Definições',
  'Logout': 'Terminar sessão',
  'Login': 'Iniciar sessão',
  'Username': 'Utilizador',
  'Password': 'Palavra-passe',
  'Submit': 'Entrar',
  'Invalid credentials': 'Credenciais inválidas',
  'Language': 'Idioma',
  'Theme': 'Tema',
  'Light': 'Claro',
  'Dark': 'Escuro',
  'Light mode': 'Modo claro',
  'Dark mode': 'Modo escuro',
  'Application Settings': 'Definições da Aplicação',
  'Active profile': 'Perfil ativo',
  'Project': 'Projeto',
  'Organisation': 'Organização',
  'Role': 'Perfil',
  'Name': 'Nome',
  'Job Title': 'Cargo',
  'Accessibility': 'Acessibilidade',
  'Portuguese': 'Português',
  'English': 'Inglês',
  'Overview': 'Visão geral',
  'Loading DriveTrace Core data...': 'A carregar dados do DriveTrace Core...',
  'Quality status': 'Estado de qualidade',
  'Create account': 'Criar conta',
  'Register new user': 'Registar novo utilizador',
  'Already have an account?': 'Já tem conta?',
  'Name / full name': 'Nome',
  'Email': 'Email',
  'Confirm password': 'Confirmar palavra-passe',
  'Create user': 'Criar utilizador',
  'Edit': 'Editar',
  'Delete': 'Eliminar',
  'Save': 'Guardar',
  'Cancel': 'Cancelar',
  'User saved': 'Utilizador guardado',
  'User removed': 'Utilizador removido',
  'Username already exists': 'Utilizador já existe',
  'Passwords do not match': 'As palavras-passe não coincidem',
  'All fields are required': 'Todos os campos são obrigatórios',
  'Administrator': 'Administrador',
  'Operator': 'Funcionário/Operador',
  'Client': 'Cliente',
  'Operational forecasts': 'Previsões operacionais',
  'This section prepares future analysis of completion times, delay risk and productive deviations. In this V1 the data is demonstrative.': 'Esta secção prepara a futura análise de tempos de conclusão, risco de atraso e desvios produtivos. Nesta V1, os dados são demonstrativos.',
  'Model': 'Modelo',
  'Last update': 'Última atualização',
  'Confidence': 'Confiança',
  'Future-ready placeholder': 'Base preparada para evolução futura',
  'No predictions available yet.': 'Ainda não existem previsões disponíveis.',
  'Connected to DriveTrace Core API': 'Ligado à API DriveTrace Core',
  'Offline demo data loaded': 'Dados demo offline carregados',
  'Connecting to API...': 'A ligar à API...',
  'Executing playback...': 'A executar reprodução...',
  'Playback executed. Dashboard data refreshed.': 'Reprodução executada. Dados atualizados.',
  'Sending manual controlled event...': 'A enviar evento manual controlado...',
  'Manual event accepted. Dashboard data refreshed.': 'Evento manual aceite. Dados atualizados.',
  'Publishing current context to Orion-LD...': 'A publicar contexto atual no Orion-LD...',
  'FIWARE publishing request executed. If Orion-LD is running, check port 1026.': 'Pedido de publicação FIWARE executado. Se o Orion-LD estiver ativo, verificar a porta 1026.',
  'Racks only aggregate supports after the controlled line. The support remains the traceability reference for intra-line WIP.': 'As racks apenas agregam suportes depois da linha controlada. O suporte mantém-se como referência de rastreabilidade WIP intra-linha.',
  'The relational backend remains the business source of truth. Orion-LD is used for current/hot context of supports, product units and racks.': 'O backend relacional mantém-se como fonte de verdade. O Orion-LD é usado para contexto atual de suportes, unidades e racks.',
}

const dictionaries: Record<string, Record<string, string>> = { 'pt-PT': pt, en: {} }

export function t(text: string): string {
  const dictionary = dictionaries[locale.value] || {}
  return dictionary[text] || text
}

const statusPT: Record<string, string> = {
  Active: 'Ativo',
  'In Progress': 'Em progresso',
  Completed: 'Concluído',
  Blocked: 'Bloqueado',
  Rework: 'Retrabalho',
  Scrap: 'Sucata',
  Stored: 'Armazenado',
  Loaded: 'Carregado',
  Available: 'Disponível',
  Pending: 'Pendente',
  PASS: 'Aprovado',
  FAIL: 'Reprovado',
  Open: 'Aberto',
  Placeholder: 'Demonstração',
}

const sectionPT: Record<string, string> = {
  'Raw Materials': 'Matérias-primas',
  'Support Assignment': 'Atribuição do suporte',
  'Blanking / Stamping / Cutting': 'Corte / Estampagem',
  'Stamping and Cutting': 'Corte e Estampagem',
  'Hemming & Welding': 'Dobra e Soldadura',
  'Hemming and Welding': 'Dobra e Soldadura',
  Painting: 'Pintura',
  'Quality Control': 'Controlo de Qualidade',
  'Quality Inspection': 'Inspeção de Qualidade',
  'Rack Storage': 'Armazenamento em Rack',
  'Post-line Storage': 'Armazenamento Pós-Linha',
  Warehouse: 'Armazém',
  Tracking: 'Rastreio',
  Production: 'Produção',
  Quality: 'Qualidade',
  'Post-line Logistics': 'Logística Pós-Linha',
}

const materialPT: Record<string, string> = {
  'Steel Sheet': 'Chapa de aço',
  'Aluminium Panel': 'Painel de alumínio',
  'Paint Primer': 'Primário de pintura',
  'Final Paint': 'Tinta final',
  'Rubber Seal': 'Vedante de borracha',
  'Wiring Clip': 'Clip de cablagem',
}

const miscPT: Record<string, string> = {
  Subproduct: 'Subproduto',
  Final: 'Final',
  Movement: 'Movimento',
  QualityFailure: 'Falha de qualidade',
  SupportAssigned: 'Suporte atribuído',
  Consumed: 'Consumido',
  Major: 'Maior',
  Medium: 'Médio',
}

export function translateStatus(status: string | undefined): string {
  if (!status) return ''
  if (locale.value === 'pt-PT') return statusPT[status] || miscPT[status] || status
  return status
}

export function translateSectionName(section: string | undefined): string {
  if (!section) return ''
  if (locale.value === 'pt-PT') return sectionPT[section] || section
  return section
}

export function translateMaterialName(material: string | undefined): string {
  if (!material) return ''
  if (locale.value === 'pt-PT') return materialPT[material] || material
  return material
}

export function translateUnitType(type: string | undefined): string {
  if (!type) return ''
  if (locale.value === 'pt-PT') return miscPT[type] || type
  return type
}

export function translateQualityResult(result: string | undefined): string {
  return translateStatus(result)
}
