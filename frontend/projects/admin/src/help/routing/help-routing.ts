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
  markdown: string;
}

export const DEFAULT_HELP_TOPIC: HelpTopic = 'welcome';

export const HELP_TOPICS: Record<HelpTopic, HelpTopicContent> = {
  welcome: {
    title: 'Välkommen till Conclave',
    markdown: `
## Välkommen till Conclave

Här hittar du kort hjälp för den vy du arbetar i. Välj ett ämne i listan eller använd hjälpknappen igen från en annan sida.

`,
  },
  'convention-overview': {
    title: 'Konvent och upplagor',
    markdown: `
## Konvent och upplagor

Konventet är den övergripande organisationen. En upplaga är en specifik genomförandeperiod med egna datum, koordinatorer och struktur.

På startsidan skapar, importerar och öppnar du upplagor.
`,
  },
  'edition-basics': {
    title: 'Upplagans grunduppgifter',
    markdown: `
## Upplagans grunduppgifter

Grunduppgifterna styr namn, datum och ansvariga koordinatorer för upplagan.

Schematider per dag används som stöd när andra flöden föreslår tider.
`,
  },
  'edition-lifecycle': {
    title: 'Upplagans livscykel',
    markdown: `
## Upplagans livscykel

Publicering gör upplagan synlig i publika flöden. Aktiv upplaga används som standard i admin och publika vyer.

Registreringsöppningarna styr vilka grupper som kan anmäla sig just nu.
`,
  },
  'edition-structure': {
    title: 'Upplagans struktur',
    markdown: `
## Upplagans struktur

Lokaler, funktionsområden, stationer, kategorier och biljettyper hör till en upplaga.

Bygg strukturen innan program, bemanning och registreringar börjar användas fullt ut.
`,
  },
  'event-workflow': {
    title: 'Evenemang och schema',
    markdown: `
## Evenemang och schema

Evenemang beskriver programpunkter. Sessioner placerar dem i tid och lokal.

Arrangörsflödet och granskningsflödet avgör när programpunkter är redo att schemaläggas.
`,
  },
  registration: {
    title: 'Registrering och besökare',
    markdown: `
## Registrering och besökare

Besökarregistreringar, biljetter och promotionkoder hör till registreringsflödet.

Kontrollera att rätt registreringsfönster är öppet innan du felsöker en saknad anmälan.
`,
  },
  staff: {
    title: 'Funktionärer och bemanning',
    markdown: `
## Funktionärer och bemanning

Funktionärer ansöker till områden och kan tilldelas pass på stationer.

Bemanningsvyerna utgår från aktiv upplaga.
`,
  },
  feeds: {
    title: 'Publika feeds',
    markdown: `
## Publika feeds

Feeds används för att exponera programdata till externa eller publika konsumenter.

URL:erna bygger på aktuell konventionskontext.
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
