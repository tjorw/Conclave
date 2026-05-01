export type HelpTopic =
  | 'welcome'
  | 'convention-overview'
  | 'edition-basics'
  | 'edition-lifecycle'
  | 'edition-structure'
  | 'event-workflow'
  | 'registration'
  | 'staff'
  | 'feeds';

export interface HelpTopicContent {
  title: string;
  assetPath?: string;
  fallbackMarkdown: string;
}

export const DEFAULT_HELP_TOPIC: HelpTopic = 'welcome';

export const HELP_TOPICS: Record<HelpTopic, HelpTopicContent> = {
  welcome: {
    title: 'Välkommen till Conclave',
    fallbackMarkdown: `
## Välkommen till Conclave

Här hittar du kort hjälp för den vy du arbetar i. Välj ett ämne i listan eller använd hjälpknappen från en specifik vy för kontextuell hjälp.
`,
  },
  'convention-overview': {
    title: 'Konvent och upplagor',
    assetPath: 'assets/help/convention/overview.md',
    fallbackMarkdown: `
## Konvent och upplagor

Konventet är den övergripande organisationen. En upplaga är en specifik genomförandeperiod med egna datum, koordinatorer och struktur.
`,
  },
  'edition-basics': {
    title: 'Upplagans grunduppgifter',
    assetPath: 'assets/help/convention/edition-basics.md',
    fallbackMarkdown: `
## Upplagans grunduppgifter

Grunduppgifterna styr namn, datum och ansvariga koordinatorer för upplagan.
`,
  },
  'edition-lifecycle': {
    title: 'Upplagans livscykel',
    assetPath: 'assets/help/convention/edition-lifecycle.md',
    fallbackMarkdown: `
## Upplagans livscykel

Publicering gör upplagan synlig i publika flöden. Registreringsöppningarna styr vilka grupper som kan anmäla sig.
`,
  },
  'edition-structure': {
    title: 'Upplagans struktur',
    assetPath: 'assets/help/convention/edition-basics.md',
    fallbackMarkdown: `
## Upplagans struktur

Lokaler, funktionsområden, stationer, kategorier och biljettyper hör till en upplaga.
`,
  },
  'event-workflow': {
    title: 'Evenemang och schema',
    assetPath: 'assets/help/event/workflow.md',
    fallbackMarkdown: `
## Evenemang och schema

Evenemang beskriver programpunkter. Sessioner placerar dem i tid och lokal.
`,
  },
  registration: {
    title: 'Registrering och besökare',
    assetPath: 'assets/help/registration/overview.md',
    fallbackMarkdown: `
## Registrering och besökare

Besökarregistreringar, biljetter och promotionkoder hör till registreringsflödet.
`,
  },
  staff: {
    title: 'Funktionärer och bemanning',
    assetPath: 'assets/help/staff/overview.md',
    fallbackMarkdown: `
## Funktionärer och bemanning

Funktionärer ansöker till områden och kan tilldelas pass på stationer.
`,
  },
  feeds: {
    title: 'Publika feeds',
    fallbackMarkdown: `
## Publika feeds

Feeds används för att exponera programdata till externa eller publika konsumenter.
`,
  },
};

export const HELP_TOPIC_ORDER: HelpTopic[] = [
  'welcome',
  'convention-overview',
  'edition-basics',
  'edition-lifecycle',
  'edition-structure',
  'event-workflow',
  'registration',
  'staff',
  'feeds',
];

export const HELP_ROUTE_MAP: readonly { pattern: RegExp; topic: HelpTopic }[] = [
  { pattern: /^\/dashboard(?:$|[/?#])/, topic: 'convention-overview' },
  { pattern: /^\/editions\/[^/]+\/basics(?:$|[/?#])/, topic: 'edition-basics' },
  { pattern: /^\/editions\/[^/]+\/lifecycle(?:$|[/?#])/, topic: 'edition-lifecycle' },
  { pattern: /^\/editions\/[^/]+\/(?:venues|staff-areas|categories|ticket-types|export)(?:$|[/?#])/, topic: 'edition-structure' },
  { pattern: /^\/(?:events|sessions|persons\/organisers)(?:$|[/?#])/, topic: 'event-workflow' },
  { pattern: /^\/(?:persons\/visitors|registrations)(?:$|[/?#])/, topic: 'registration' },
  { pattern: /^\/(?:persons\/staff|staff-areas|staffing-schedule)(?:$|[/?#])/, topic: 'staff' },
  { pattern: /^\/feeds(?:$|[/?#])/, topic: 'feeds' },
];

export function topicForRoute(url: string): HelpTopic {
  const path = url.split(/[?#]/, 1)[0] || '/';
  return HELP_ROUTE_MAP.find(entry => entry.pattern.test(path))?.topic ?? DEFAULT_HELP_TOPIC;
}
